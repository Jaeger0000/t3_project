using T3.Domain.Common;
using T3.Domain.Startups;

namespace T3.Domain.Registrations;

/// <summary>
/// Ana sayfadaki "Kayıt Ol" formundan gelen, onay bekleyen girişim başvurusu.
/// Girişim kullanıcısı hiçbir tabloya doğrudan yazamadığı için kayıt olma da
/// bir istisna değil: başvuru SuperAdmin onaylayana kadar ne <see cref="Startup"/>
/// ne de kullanıcı satırı var olur — yalnızca bu ara kayıt.
/// </summary>
public class StartupRegistrationRequest : Entity, IAuditable
{
    public string Email { get; set; } = null!;

    /// <summary>
    /// Kullanıcının formda seçtiği şifrenin özeti. Onay anında aynen kullanıcı
    /// hesabına taşınır — böylece admin şifreyi hiç bilmez, kendi belirlediği
    /// şifreyle ilk günden giriş yapar.
    /// </summary>
    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;
    public string StartupName { get; set; } = null!;
    public Sector Sector { get; set; }
    public string? City { get; set; }
    public string? ContactPhone { get; set; }

    public RegistrationRequestStatus Status { get; set; } = RegistrationRequestStatus.Pending;

    public DateTimeOffset? ReviewedAt { get; set; }

    /// <summary>Navigasyon yok — yalnızca "kim inceledi" bilgisi, denetim izinin ayrıntısı.</summary>
    public Guid? ReviewedByUserId { get; set; }

    public string? RejectionReason { get; set; }

    /// <summary>Onaylandıysa oluşan girişimin kimliği; ret/bekleyen durumda boş.</summary>
    public Guid? CreatedStartupId { get; set; }

    /// <summary>Onaylandıysa oluşan kullanıcının kimliği; ret/bekleyen durumda boş.</summary>
    public Guid? CreatedUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
