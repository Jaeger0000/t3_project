using Microsoft.AspNetCore.Mvc;
using T3.Api.Authorization;
using T3.Api.Http;
using T3.Api.RateLimiting;
using T3.Application.Features.Documents.DeleteDocument;
using T3.Application.Features.Documents.DownloadDocument;
using T3.Application.Features.Documents.ListDocuments;
using T3.Application.Features.Documents.UploadDocument;
using T3.Domain.Documents;

namespace T3.Api.Endpoints;

/// <summary>
/// Doküman yükleme ve indirme (MVP #4).
///
/// Yükleme ucu <c>.RequireAuthorization()</c> ile yalnızca kimlik ister, rol
/// istemez: yetkili doğrudan kaydeder, girişim kullanıcısı onay isteği üretir.
/// Ayrımı politika değil handler yapıyor çünkü karar satır düzeyinde
/// ("bu girişim benim mi") ve politika hattı satırı göremez.
/// </summary>
public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/startups/{id:guid}/documents").WithTags("Documents");

        group.MapGet("/", async (
                Guid id,
                ListDocumentsHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToHttp())
            .WithSummary("Girişimin dokümanlarını listeler (yetkisiz rol listeyi göremez).");

        group.MapPost("/", async (
                Guid id,
                IFormFile file,
                DocumentType? type,
                UploadDocumentHandler handler,
                CancellationToken ct) =>
            {
                await using var stream = file.OpenReadStream();

                var request = new DocumentUploadRequest(
                    id,
                    type ?? DocumentType.Other,
                    file.FileName,
                    file.Length,
                    stream);

                return (await handler.Handle(request, ct)).ToHttp();
            })
            // Bu, ASP.NET'in form-bound uçlara otomatik uyguladığı YERLEŞİK
            // antiforgery kontrolünü kapatır — CsrfProtection ara katmanıyla
            // (çift-gönderim jetonu, X-CSRF-Token) karıştırılmamalı, o ayrı ve
            // hâlâ devrede. Yanıltıcı olan eski gerekçe şuydu: "API Bearer
            // jetonuyla kimlik doğruluyor, çerez yok, CSRF yüzeyi yok" — Dalga
            // 2'den beri tarayıcı **çerezle** kimlik doğruluyor
            // (bkz. G-21, Guvenlik_Denetimi_ve_Iyilestirme_Plani.md). Bu uç
            // bugün de korunuyor ama koruyan şey CsrfProtection middleware'i:
            // çerezle gelen bir yükleme isteği doğru X-CSRF-Token başlığı
            // olmadan 403 alır (bkz. CsrfProtection.Invoke). Yerleşik
            // antiforgery'nin kapalı kalmasının sebebi bambaşka: o mekanizma
            // ayrı bir form-jetonu ister ve Bearer/MCP/betik istemcilerinin
            // hiçbiri onu taşımaz; açık bırakmak yalnızca onlar için 400 üretirdi.
            .DisableAntiforgery()
            // DocumentUploadRules.MaxSizeBytes (20 MB) uygulama düzeyinde bir
            // kural; buradaki 21 MB'lık sunucu sınırı ondan önce devreye girer
            // — aksi hâlde ASP.NET tüm gövdeyi belleğe/diske aldıktan SONRA
            // uygulama kuralı reddediyordu (bkz. G-08,
            // Guvenlik_Denetimi_ve_Iyilestirme_Plani.md). nginx'teki
            // client_max_body_size bu sınırdan büyük tutulmalı (bkz. ops notu).
            .WithMetadata(new RequestSizeLimitAttribute(21_000_000))
            .WithMetadata(new RequestFormLimitsAttribute { MultipartBodyLengthLimit = 21_000_000 })
            .WithSummary("Doküman yükler; yetkiliyse kaydeder, girişim kullanıcısıysa onaya gönderir.");

        group.MapDelete("/{documentId:guid}", async (
                Guid id,
                Guid documentId,
                DeleteDocumentHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, documentId, ct)).ToNoContent())
            .RequireAuthorization(Policies.ManageStartups)
            .WithSummary("Dokümanı pasife alır (dosya depoda kalır).");

        // İndirme girişim kimliğinden bağımsız: bağlantı paylaşılabilir olduğu
        // için yetki her çağrıda yeniden kontrol ediliyor.
        app.MapGet("/api/documents/{documentId:guid}/download", async (
                Guid documentId,
                DownloadDocumentHandler handler,
                HttpContext http,
                CancellationToken ct) =>
            {
                var result = await handler.Handle(documentId, ct);

                if (!result.IsSuccess)
                    return ApiResults.Problem(result.Error!);

                var file = result.Value!;

                // Tarayıcı içerik tipini tahmin etmesin: yüklenen dosya
                // saldırganın seçtiği içerik olabilir, sayfa bağlamında
                // çalıştırılmasının önü kapatılıyor.
                http.Response.Headers["X-Content-Type-Options"] = "nosniff";

                return Results.File(file.Content, file.ContentType, file.FileName);
            })
            .RequireRateLimiting(AuthRateLimit.MassExportPolicy)
            .WithTags("Documents")
            .WithSummary("Dokümanı indirir; her indirme denetim izine yazılır.");

        return app;
    }
}
