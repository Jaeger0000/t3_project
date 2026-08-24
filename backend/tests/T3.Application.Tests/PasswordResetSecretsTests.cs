using T3.Application.Features.Auth;

namespace T3.Application.Tests;

/// <summary>
/// Sıfırlama jetonunun iki değişmezi: URL'de bozulmadan taşınabilmeli ve
/// veritabanında yalnızca özeti durmalı — yedek dosyasını okuyan biri hiçbir
/// hesabın şifresini sıfırlayamamalı.
/// </summary>
public class PasswordResetSecretsTests
{
    [Fact]
    public void Jeton_url_guvenli_karakterlerden_olusur()
    {
        var token = PasswordResetSecrets.CreateRawToken();

        Assert.DoesNotContain('+', token);
        Assert.DoesNotContain('/', token);
        Assert.DoesNotContain('=', token);
        Assert.True(token.Length >= 43, $"jeton beklenenden kısa: {token.Length}");
    }

    [Fact]
    public void Her_jeton_farklidir()
    {
        var tokens = Enumerable.Range(0, 50)
            .Select(_ => PasswordResetSecrets.CreateRawToken())
            .ToHashSet();

        Assert.Equal(50, tokens.Count);
    }

    [Fact]
    public void Ozet_jetonun_kendisini_tasimaz()
    {
        var token = PasswordResetSecrets.CreateRawToken();
        var hash = PasswordResetSecrets.Hash(token);

        Assert.DoesNotContain(token, hash);
        // SHA-256, onaltılık: kolon uzunluğu (64) bu sayıya göre verildi.
        Assert.Equal(64, hash.Length);
    }

    [Fact]
    public void Ayni_jeton_ayni_ozeti_uretir()
    {
        var token = PasswordResetSecrets.CreateRawToken();

        Assert.Equal(PasswordResetSecrets.Hash(token), PasswordResetSecrets.Hash(token));
    }

    [Fact]
    public void Baglanti_omru_kisa_tutulur()
    {
        Assert.InRange(PasswordResetSecrets.Lifetime.TotalHours, 0.5, 24);
    }
}
