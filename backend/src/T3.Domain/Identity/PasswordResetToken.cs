using T3.Domain.Common;

namespace T3.Domain.Identity;

/// <summary>
/// Şifre sıfırlama jetonu.
///
/// Jetonun <b>kendisi saklanmıyor</b>, yalnızca özeti: veritabanı okuyan biri
/// (yedek dosyası, log, SQL erişimi) hiçbir hesabın şifresini sıfırlayamamalı.
/// Tek kullanımlık olması <see cref="UsedAt"/> ile, süreli olması
/// <see cref="ExpiresAt"/> ile taşınıyor; ikisi de kayıt üzerinde duruyor
/// çünkü kullanılmış jetonu silmek "bu bağlantı zaten kullanıldı" cevabını
/// vermeyi imkânsız kılardı.
/// </summary>
public class PasswordResetToken : Entity, IAuditable
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Ham jetonun SHA-256 özeti (onaltılık).</summary>
    public string TokenHash { get; set; } = null!;

    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }

    /// <summary>Talebin geldiği adres — kötüye kullanım incelemesi için.</summary>
    public string? RequestedFromIp { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
