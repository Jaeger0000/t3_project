using T3.Domain.Common;
using T3.Domain.Identity;
using T3.Domain.Startups;

namespace T3.Domain.Approvals;

public enum ChangeTargetType
{
    Startup = 1,
    TeamMember = 2,
    Achievement = 3,
    Document = 4
}

public enum ChangeOperation
{
    Create = 1,
    Update = 2,
    Delete = 3
}

public enum ChangeRequestStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}

/// <summary>
/// Onay akışının merkezi (MVP #3). Startup kullanıcısı hiçbir tabloya doğrudan
/// yazmaz; önerdiği değişiklik burada bekler ve ancak yetkili onayladıktan
/// sonra hedef varlığa uygulanır.
/// </summary>
public class ChangeRequest : Entity, ISoftDelete
{
    public Guid StartupId { get; set; }
    public Startup Startup { get; set; } = null!;

    public Guid SubmittedByUserId { get; set; }
    public User SubmittedBy { get; set; } = null!;
    public DateTimeOffset SubmittedAt { get; set; }

    public ChangeTargetType TargetType { get; set; }

    /// <summary>Güncelleme/silme için hedef kaydın kimliği; yeni kayıt önerisinde null.</summary>
    public Guid? TargetId { get; set; }

    public ChangeOperation Operation { get; set; }

    /// <summary>Önerilen yeni değerler (JSON).</summary>
    public string PayloadJson { get; set; } = null!;

    /// <summary>
    /// Gönderim anındaki mevcut değerler (JSON). Onay ekranındaki
    /// "önce / sonra" karşılaştırması bu alandan üretilir.
    /// </summary>
    public string? BeforeJson { get; set; }

    public ChangeRequestStatus Status { get; set; } = ChangeRequestStatus.Pending;

    public Guid? ReviewedByUserId { get; set; }
    public User? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }

    /// <summary>Girişim pasife alındığında birlikte işaretlenir.</summary>
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
