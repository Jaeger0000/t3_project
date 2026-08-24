using T3.Application.Features.Auth.ChangePassword;
using T3.Application.Features.Auth.ResetPassword;

namespace T3.Application.Tests;

/// <summary>
/// Şifre değiştirme ve sıfırlama aynı politikayı paylaşıyor: biri gevşek
/// kalırsa gevşek kapıdan girilen şifreyle diğerinin sıkılığı anlamsızlaşır.
/// </summary>
public class PasswordChangeRulesTests
{
    private static readonly ChangePasswordValidator ChangeValidator = new();
    private static readonly ResetPasswordValidator ResetValidator = new();

    [Fact]
    public void Yeni_sifre_mevcut_sifreyle_ayni_olamaz()
    {
        var result = ChangeValidator.Validate(
            new ChangePasswordRequest("Guclu.Sifre1", "Guclu.Sifre1"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("farklı olmalıdır"));
    }

    [Fact]
    public void Kisa_yeni_sifre_reddedilir()
    {
        var result = ChangeValidator.Validate(new ChangePasswordRequest("Guclu.Sifre1", "kisa1"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Gecerli_degisiklik_kabul_edilir()
    {
        var result = ChangeValidator.Validate(
            new ChangePasswordRequest("Guclu.Sifre1", "Baska.Sifre9"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Sifirlamada_jeton_zorunlu()
    {
        var result = ResetValidator.Validate(new ResetPasswordRequest("", "Baska.Sifre9"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("bağlantı"));
    }

    [Fact]
    public void Sifirlamada_da_sifre_politikasi_isliyor()
    {
        var result = ResetValidator.Validate(new ResetPasswordRequest("jeton", "sadeceharf"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("rakam"));
    }
}
