using T3.Api.Filters;
using T3.Api.Http;
using T3.Application.Features.Assistant.AskAssistant;
using T3.Application.Features.Assistant.Chat;
using T3.Application.Features.Assistant.Chat.GetConversation;
using T3.Application.Features.Assistant.Chat.ListConversations;
using T3.Application.Features.Assistant.DownloadExport;
using T3.Application.Features.Assistant.SummarizeStartup;

namespace T3.Api.Endpoints;

/// <summary>
/// AI karar destek uçları. Hız sınırı "ai" kovasına bağlı: sohbet ucu dış
/// sağlayıcıya istek üretebiliyor, sınırsız bırakılamaz.
/// </summary>
public static class AssistantEndpoints
{
    public static IEndpointRouteBuilder MapAssistantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai").WithTags("AI").RequireRateLimiting("ai");

        group.MapPost("/ask", async (
                AskAssistantRequest request,
                AskAssistantHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(request, ct)).ToHttp())
            .WithValidation<AskAssistantRequest>()
            .WithSummary("Doğal dil ekosistem sorgusu; yanıt dayandığı kayıtları da döner.");

        // Çok turlu sohbet. Bağlam sunucuda tutuluyor; istemci geçmişi
        // göndermediği için "hiç sorulmamış turu sorulmuş gibi sunma" yolu
        // kapalı. Sahiplik kontrolü handler'da (bkz. ChatHandler).
        group.MapPost("/chat", async (
                ChatRequest request,
                ChatHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(request, ct)).ToHttp())
            .WithValidation<ChatRequest>()
            .WithSummary("Sohbete bir tur ekler; sohbet kimliği boşsa yeni sohbet açar.");

        group.MapGet("/chat/conversations", async (
                int? page,
                int? pageSize,
                ListConversationsHandler handler,
                CancellationToken ct) =>
            {
                var request = new ListConversationsRequest
                {
                    Page = page ?? 1,
                    PageSize = pageSize ?? 20
                };

                return (await handler.Handle(request, ct)).ToHttp();
            })
            .WithSummary("Çağıranın kendi sohbetleri; en son konuşulan önce.");

        group.MapGet("/chat/conversations/{id:guid}", async (
                Guid id,
                GetConversationHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToHttp())
            .WithSummary("Tek sohbetin turları; başkasının sohbeti 404 döner.");

        // Asistanın araç turunda ürettiği dosya (ör. Excel dışa aktarma).
        // Jeton tek kullanımlık; ikinci istekte 404 döner.
        group.MapGet("/exports/{token}", (
                string token,
                DownloadAssistantExportHandler handler,
                HttpContext http) =>
            {
                var result = handler.Handle(token);
                if (!result.IsSuccess)
                    return ApiResults.Problem(result.Error!);

                var file = result.Value!;
                http.Response.Headers["X-Content-Type-Options"] = "nosniff";
                return Results.File(file.Content, file.ContentType, file.FileName);
            })
            .WithSummary("Asistanın ürettiği dosyayı indirir (ör. Excel dışa aktarma).");

        group.MapGet("/startups/{id:guid}/summary", async (
                Guid id,
                SummarizeStartupHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToHttp())
            .WithSummary("Girişim kartı için yönetici özeti üretir (maskeleme korunur).");

        return app;
    }
}
