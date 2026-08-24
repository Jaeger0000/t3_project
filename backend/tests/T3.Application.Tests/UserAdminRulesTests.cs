using T3.Application.Features.Users;
using T3.Application.Features.Users.CreateUser;
using T3.Application.Features.Users.SetUserPassword;
using T3.Domain.Identity;

namespace T3.Application.Tests;

/// <summary>
/// Rol ile kapsam bağının kuralları. Yetkilendirmenin tamamı bu bağa dayanıyor:
/// kapsamsız bir Program Yöneticisi hiçbir şey göremez, girişimsiz bir portal
/// kullanıcısı öneri gönderemez — ikisi de sessizce işlevsiz hesap üretir.
/// </summary>
public class UserAdminRulesTests
{
    private static readonly Guid ProgramId = Guid.NewGuid();
    private static readonly Guid StartupId = Guid.NewGuid();

    [Fact]
    public void GirisimKullanicisi_girisim_olmadan_olusturulamaz()
    {
        Assert.NotNull(UserAdminGuard.ValidateBinding(UserRole.StartupUser, null, null));
    }

    [Fact]
    public void GirisimKullanicisina_program_atanamaz()
    {
        Assert.NotNull(UserAdminGuard.ValidateBinding(
            UserRole.StartupUser, StartupId, [ProgramId]));
    }

    [Fact]
    public void GirisimKullanicisi_girisimle_gecerlidir()
    {
        Assert.Null(UserAdminGuard.ValidateBinding(UserRole.StartupUser, StartupId, null));
    }

    [Fact]
    public void ProgramYoneticisi_en_az_bir_program_ister()
    {
        Assert.NotNull(UserAdminGuard.ValidateBinding(UserRole.ProgramManager, null, []));
        Assert.Null(UserAdminGuard.ValidateBinding(UserRole.ProgramManager, null, [ProgramId]));
    }

    [Fact]
    public void ProgramYoneticisi_girisime_baglanamaz()
    {
        Assert.NotNull(UserAdminGuard.ValidateBinding(
            UserRole.ProgramManager, StartupId, [ProgramId]));
    }

    [Theory]
    [InlineData(UserRole.SuperAdmin)]
    [InlineData(UserRole.DecisionMaker)]
    public void Kapsamsiz_roller_atama_kabul_etmez(UserRole role)
    {
        Assert.Null(UserAdminGuard.ValidateBinding(role, null, null));
        Assert.NotNull(UserAdminGuard.ValidateBinding(role, StartupId, null));
        Assert.NotNull(UserAdminGuard.ValidateBinding(role, null, [ProgramId]));
    }

    /// <summary>
    /// Doğrulayıcı ile handler aynı kuralı kullanıyor; bu test ikisinin
    /// ayrışmadığını uçtan uca değil, kural seviyesinde kilitliyor.
    /// </summary>
    [Fact]
    public void Olusturma_dogrulayicisi_rol_kapsam_kuralini_uygular()
    {
        var validator = new CreateUserValidator();

        var invalid = new CreateUserRequest(
            "yeni@ornek.test", "Yeni Kullanıcı", UserRole.StartupUser,
            "Guclu.Sifre1", StartupId: null, ProgramIds: null);

        Assert.False(validator.Validate(invalid).IsValid);
        Assert.True(validator.Validate(invalid with { StartupId = StartupId }).IsValid);
    }

    [Theory]
    [InlineData("kisa1", false)]           // 10 karakterden kısa
    [InlineData("sadeceharfler", false)]   // rakam yok
    [InlineData("1234567890", false)]      // harf yok
    [InlineData("Guclu.Sifre1", true)]
    public void Sifre_politikasi(string password, bool expected)
    {
        var validator = new SetUserPasswordValidator();

        Assert.Equal(
            expected, validator.Validate(new SetUserPasswordRequest(password)).IsValid);
    }
}
