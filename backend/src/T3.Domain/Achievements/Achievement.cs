using T3.Domain.Common;
using T3.Domain.Startups;

namespace T3.Domain.Achievements;

/// <summary>
/// Girişimin finansal ve başarı kayıtlarının ortak temeli (MVP #4).
/// Brief "serbest metin değil, alan bazlı veri modeli" istediği için her kayıt
/// tipi kendi güçlü tipli sınıfına sahip; EF Core bunları tek tabloda
/// (TPH — Table Per Hierarchy) ayrıştırıcı kolonla saklar.
/// </summary>
public abstract class Achievement : Entity, IAuditable, ISoftDelete
{
    public Guid StartupId { get; set; }
    public Startup Startup { get; set; } = null!;

    /// <summary>Kaydın gerçekleştiği tarih — zaman çizelgesinde sıralama anahtarı.</summary>
    public DateOnly OccurredOn { get; set; }

    public string? Note { get; set; }

    /// <summary>
    /// Kayıt bir yetkili tarafından doğrulandı mı? Girişimin kendi girdiği
    /// veriler onay akışından geçmeden doğrulanmış sayılmaz.
    /// </summary>
    public bool IsVerified { get; set; }
    public Guid? VerifiedByUserId { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
