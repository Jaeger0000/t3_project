using System.Text.Json;
using T3.Application.Common.Interfaces;
using T3.Domain.Audit;
using T3.Domain.Identity;

namespace T3.Infrastructure.Audit;

public sealed class AuditWriter(
    IAppDbContext db,
    ICurrentUser currentUser,
    IClientContext client) : IAuditWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public Task WriteAsync(
        string action,
        string entityType,
        Guid? entityId,
        object? before = null,
        object? after = null,
        CancellationToken ct = default) =>
        WriteForActorAsync(
            currentUser.UserId, currentUser.Role,
            action, entityType, entityId, before, after, ct);

    public async Task WriteForActorAsync(
        Guid? actorUserId,
        UserRole? actorRole,
        string action,
        string entityType,
        Guid? entityId,
        object? before = null,
        object? after = null,
        CancellationToken ct = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            // Aktör kimliği boş bırakılabiliyor: başarısız girişte kimlik
            // doğrulanmamıştır ve uydurma bir kimlik yazmak izi yanıltır.
            // Eskiden rol boşken DecisionMaker yazılıyordu — kayıt, hiç var
            // olmayan bir rolü olay yapmış gibi görünüyordu.
            ActorUserId = actorUserId,
            ActorRole = actorRole,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            BeforeJson = Serialize(before),
            AfterJson = Serialize(after),
            IpAddress = client.IpAddress,
            UserAgent = client.UserAgent,
            OccurredAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(ct);
    }

    private static string? Serialize(object? value) =>
        value is null ? null : JsonSerializer.Serialize(value, JsonOptions);
}
