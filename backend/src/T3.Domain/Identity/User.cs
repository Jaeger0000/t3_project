using T3.Domain.Common;
using T3.Domain.Startups;

namespace T3.Domain.Identity;

public class User : Entity, IAuditable, ISoftDelete
{
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public UserRole Role { get; set; }

    /// <summary>
    /// Yalnızca <see cref="UserRole.StartupUser"/> için dolu; kullanıcıyı
    /// yönetebileceği tek girişime bağlar.
    /// </summary>
    public Guid? StartupId { get; set; }
    public Startup? Startup { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>
    /// Jetona gömülür ve her istekte canlı değerle karşılaştırılır
    /// (bkz. <c>IUserStateProvider</c>). Pasife alma, rol/program değişikliği
    /// ve şifre değişikliğinde yenilenir — eski jetonu elinde tutan biri bir
    /// sonraki istekte 401 alır, giriş akışını beklemek zorunda kalmaz
    /// (bkz. G-01, Guvenlik_Denetimi_ve_Iyilestirme_Plani.md).
    /// </summary>
    public Guid SecurityStamp { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Art arda yanlış şifre denemesi sayacı. Başarılı girişte sıfırlanır.
    /// 10'a ulaşınca <see cref="LockedUntil"/> ayarlanır ve sayaç sıfırlanır
    /// (bkz. G-05, Guvenlik_Denetimi_ve_Iyilestirme_Plani.md). Hız sınırı
    /// (IP+e-posta, dakikada 10) tek bir IP'den yapılan denemeyi zaten
    /// yavaşlatıyor; bu sayaç aynı hesabı zaman içine yayılmış ya da farklı
    /// IP'lerden gelen denemelere karşı korur.
    /// </summary>
    public int FailedLoginCount { get; set; }

    /// <summary>Doluysa ve gelecekteyse hesap kilitlidir — şifre doğru olsa da giriş reddedilir.</summary>
    public DateTimeOffset? LockedUntil { get; set; }

    /// <summary>
    /// Yönetici şifre atadığında <c>true</c> olur, kullanıcı kendi şifresini
    /// belirlediğinde <c>false</c>. Amaç yöneticinin bildiği şifrenin kalıcı
    /// olmaması: aksi hâlde her hesabın şifresini bir başkası da biliyor.
    /// </summary>
    public bool MustChangePassword { get; set; }

    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = [];

    /// <summary>Program Yöneticisi'nin yetki kapsamını belirler.</summary>
    public ICollection<UserProgramAssignment> ProgramAssignments { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
