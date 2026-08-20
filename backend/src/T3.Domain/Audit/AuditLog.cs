using T3.Domain.Common;
using T3.Domain.Identity;

namespace T3.Domain.Audit;

/// <summary>
/// KVKK denetim izi: her yazma işlemi kim, ne zaman, neyi, nasıl
/// değiştirdi sorusuna cevap verecek şekilde buraya düşer. Kayıtlar
/// değiştirilemez kabul edilir — yalnızca eklenir.
/// </summary>
public class AuditLog : Entity
{
    public Guid ActorUserId { get; set; }
    public UserRole ActorRole { get; set; }

    /// <summary>Örn. "Startup.Update", "ChangeRequest.Approve".</summary>
    public string Action { get; set; } = null!;

    public string EntityType { get; set; } = null!;
    public Guid? EntityId { get; set; }

    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }

    public string? IpAddress { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
