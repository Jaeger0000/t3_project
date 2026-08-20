using System.Text.Json;
using T3.Application.Common.Interfaces;
using T3.Domain.Audit;
using T3.Domain.Identity;

namespace T3.Infrastructure.Audit;

public sealed class AuditWriter(IAppDbContext db, ICurrentUser currentUser) : IAuditWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public async Task WriteAsync(
        string action,
        string entityType,
        Guid? entityId,
        object? before = null,
        object? after = null,
        CancellationToken ct = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUser.UserId ?? Guid.Empty,
            ActorRole = currentUser.Role ?? UserRole.DecisionMaker,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            BeforeJson = Serialize(before),
            AfterJson = Serialize(after),
            OccurredAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(ct);
    }

    private static string? Serialize(object? value) =>
        value is null ? null : JsonSerializer.Serialize(value, JsonOptions);
}
