using T3.Api.Authorization;
using T3.Api.Http;
using T3.Application.Features.Audit.ListAuditLogs;

namespace T3.Api.Endpoints;

/// <summary>
/// Denetim izi ucu. Politika SuperAdmin'e kilitli; handler aynı kontrolü
/// bağımsız olarak tekrarlıyor — iz maskelenmemiş kişisel veri içerdiği için
/// tek katmanlı korumaya bırakılmıyor.
/// </summary>
public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/audit-logs", async (
                string? entityType,
                Guid? entityId,
                Guid? actorUserId,
                string? action,
                DateTimeOffset? from,
                DateTimeOffset? to,
                int? page,
                int? pageSize,
                ListAuditLogsHandler handler,
                CancellationToken ct) =>
            {
                var request = new ListAuditLogsRequest
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    ActorUserId = actorUserId,
                    Action = action,
                    From = from,
                    To = to,
                    Page = page ?? 1,
                    PageSize = pageSize ?? 20
                };

                return (await handler.Handle(request, ct)).ToHttp();
            })
            .WithTags("Audit")
            .RequireAuthorization(Policies.ViewAuditLogs)
            .WithSummary("Denetim izini süzerek döner (yalnızca sistem yöneticisi).");

        return app;
    }
}
