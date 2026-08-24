using T3.Api.Authorization;
using T3.Api.Http;
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
            // Antiforgery jetonu kapalı: API çerezle değil Bearer jetonuyla
            // kimlik doğruluyor, dolayısıyla CSRF yüzeyi yok. Açık bırakmak
            // yalnızca jetonsuz 400 üretirdi.
            .DisableAntiforgery()
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
            .WithTags("Documents")
            .WithSummary("Dokümanı indirir; her indirme denetim izine yazılır.");

        return app;
    }
}
