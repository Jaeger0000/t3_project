using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using T3.Domain.Identity;
using T3.Infrastructure.Identity;

namespace T3.Application.Tests;

/// <summary>
/// İterasyon sayısı yükseltildiğinde eski (daha zayıf) parametrelerle üretilmiş
/// hash'lerin sessizce güncellenmesi gerekiyor — aksi hâlde yükseltme yalnızca
/// yeni kayıtlara uygulanır, mevcut şifreler eski parametrelerde kalır kalır
/// (bkz. G-15).
/// </summary>
public class Pbkdf2PasswordHasherTests
{
    [Fact]
    public void Dogru_sifre_kabul_edilir()
    {
        var hasher = new Pbkdf2PasswordHasher();
        var hash = hasher.Hash("gecerli-sifre-123");

        Assert.True(hasher.Verify("gecerli-sifre-123", hash));
        Assert.False(hasher.Verify("yanlis-sifre", hash));
    }

    [Fact]
    public void Eski_dusuk_iterasyonlu_hash_basarili_dogrulamada_yukseltilir()
    {
        // "Eski" bir hash'i taklit etmek için düşük iterasyonlu ayrı bir
        // hasher kullanılıyor — Pbkdf2PasswordHasher'ın kendi sabit 600.000
        // değerinden daha düşük olduğu sürece test, gerçek yükseltmeyi
        // (100.000 → 600.000) simüle ediyor.
        var eskiHasher = new PasswordHasher<User>(
            Options.Create(new PasswordHasherOptions { IterationCount = 10_000 }));
        var eskiHash = eskiHasher.HashPassword(new User(), "gecerli-sifre-123");

        var hasher = new Pbkdf2PasswordHasher();

        var dogrulandi = hasher.VerifyAndGetRehash(
            "gecerli-sifre-123", eskiHash, out var yeniHash);

        Assert.True(dogrulandi);
        Assert.NotNull(yeniHash);
        Assert.NotEqual(eskiHash, yeniHash);

        // Yeni hash de doğru şifreyle doğrulanabiliyor ve artık yükseltme istemiyor.
        Assert.True(hasher.VerifyAndGetRehash("gecerli-sifre-123", yeniHash!, out var ikinciYukseltme));
        Assert.Null(ikinciYukseltme);
    }

    [Fact]
    public void Guncel_hash_yukseltme_istemez()
    {
        var hasher = new Pbkdf2PasswordHasher();
        var hash = hasher.Hash("gecerli-sifre-123");

        var dogrulandi = hasher.VerifyAndGetRehash("gecerli-sifre-123", hash, out var yeniHash);

        Assert.True(dogrulandi);
        Assert.Null(yeniHash);
    }

    [Fact]
    public void Yanlis_sifrede_yukseltme_onerilmez()
    {
        var hasher = new Pbkdf2PasswordHasher();
        var hash = hasher.Hash("gecerli-sifre-123");

        var dogrulandi = hasher.VerifyAndGetRehash("yanlis-sifre", hash, out var yeniHash);

        Assert.False(dogrulandi);
        Assert.Null(yeniHash);
    }
}
