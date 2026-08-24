using System.Security.Cryptography;
using System.Text;

namespace T3.Application.Features.Auth;

/// <summary>
/// Sıfırlama jetonunun üretimi ve özeti.
///
/// Özet için PBKDF2 değil SHA-256 kullanılıyor: jeton 256 bitlik kriptografik
/// rastgele değer, yani sözlük saldırısına konu değil — yavaş türetme burada
/// koruma değil yalnızca gecikme olurdu. Şifreler bundan farklı ve onlar
/// <see cref="Common.Interfaces.IPasswordHasher"/> üzerinden PBKDF2 ile
/// saklanıyor.
/// </summary>
internal static class PasswordResetSecrets
{
    /// <summary>Bağlantının geçerlilik süresi. Kısa: e-posta kutusu bir sır deposu değil.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(2);

    /// <summary>
    /// URL'de taşınacağı için Base64Url: dolgu ve <c>+/</c> karakterleri yok,
    /// bağlantı kopyalanırken bozulmuyor.
    /// </summary>
    public static string CreateRawToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

    public static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
