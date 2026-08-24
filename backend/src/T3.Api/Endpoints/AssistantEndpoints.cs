using T3.Api.Filters;
using T3.Api.Http;
using T3.Application.Features.Assistant.AskAssistant;
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

        group.MapGet("/startups/{id:guid}/summary", async (
                Guid id,
                SummarizeStartupHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToHttp())
            .WithSummary("Girişim kartı için yönetici özeti üretir (maskeleme korunur).");

        return app;
    }
}
