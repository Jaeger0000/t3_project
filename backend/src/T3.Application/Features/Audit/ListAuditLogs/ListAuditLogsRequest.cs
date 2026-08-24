using T3.Application.Common.Paging;
using T3.Domain.Identity;

namespace T3.Application.Features.Audit.ListAuditLogs;

/// <summary>
/// Denetim izi süzgeçleri. "Bu girişime kim dokundu" ve "bu kullanıcı ne yaptı"
/// soruları KVKK denetiminin iki temel sorgusu; ikisi de tek uçla karşılanıyor.
/// </summary>
public sealed record ListAuditLogsRequest : PagedRequest
{
    /// <summary>Örn. "Startup", "ChangeRequest".</summary>
    public string? EntityType { get; init; }

    public Guid? EntityId { get; init; }
    public Guid? ActorUserId { get; init; }

    /// <summary>Örn. "ChangeRequest.Approve". Ön ek eşleşmesi yapılır.</summary>
    public string? Action { get; init; }

    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
}

/// <summary>
/// Bir denetim izi satırı. Önce/sonra gövdeleri ham JSON olarak döner: iz
/// kanıt niteliği taşıdığı için biçimlendirilmeden, yazıldığı hâliyle
/// gösterilir. Bu uca yalnızca SuperAdmin erişir.
/// </summary>
public sealed record AuditLogListItemResponse(
    Guid Id,
    string Action,
    string EntityType,
    Guid? EntityId,

    /// <summary>Başarısız giriş denemesinde boş: kimlik doğrulanmamıştır.</summary>
    Guid? ActorUserId,

    /// <summary>
    /// Kullanıcı sonradan silinmiş olabilir; o durumda kimlik gösterilir.
    /// Kimlik doğrulanmamış olaylarda "(kimlik doğrulanmadı)".
    /// </summary>
    string ActorName,

    UserRole? ActorRole,
    string? IpAddress,
    string? UserAgent,
    DateTimeOffset OccurredAt,
    string? BeforeJson,
    string? AfterJson);
