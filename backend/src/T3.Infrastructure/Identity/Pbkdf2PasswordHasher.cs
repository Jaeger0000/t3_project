using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using T3.Application.Common.Interfaces;
using T3.Domain.Identity;

namespace T3.Infrastructure.Identity;

/// <summary>
/// ASP.NET Core'un PBKDF2 tabanlı hash'leyicisini sarar. Düz metin şifre
/// hiçbir yerde saklanmaz veya loglanmaz.
///
/// İterasyon sayısı açıkça 600.000'e yükseltiliyor (OWASP'ın PBKDF2-HMAC-SHA256
/// için önerdiği 2020'ler sonu alt sınırı). ASP.NET'in kendi varsayılanı
/// (100.000) 2026 için düşük kalıyordu (bkz. G-15,
/// Guvenlik_Denetimi_ve_Iyilestirme_Plani.md).
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private static readonly IOptions<PasswordHasherOptions> Options =
        Microsoft.Extensions.Options.Options.Create(new PasswordHasherOptions { IterationCount = 600_000 });

    private readonly PasswordHasher<User> _inner = new(Options);
    private static readonly User Dummy = new();

    public string Hash(string password) => _inner.HashPassword(Dummy, password);

    public bool Verify(string password, string hash) =>
        _inner.VerifyHashedPassword(Dummy, hash, password) is
            PasswordVerificationResult.Success or
            PasswordVerificationResult.SuccessRehashNeeded;

    public bool VerifyAndGetRehash(string password, string hash, out string? rehashedHash)
    {
        var result = _inner.VerifyHashedPassword(Dummy, hash, password);

        rehashedHash = result == PasswordVerificationResult.SuccessRehashNeeded
            ? Hash(password)
            : null;

        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }

    // Sabit bir şifreden bir kez üretilip önbelleklenir: her "kullanıcı yok"
    // denemesinde yeniden hesaplamak amacın tersine, ekstra maliyet eklerdi.
    public string DummyHashForTiming { get; } = new PasswordHasher<User>(Options)
        .HashPassword(Dummy, "t3-zamanlama-dengesi-icin-sahte-sifre");
}
