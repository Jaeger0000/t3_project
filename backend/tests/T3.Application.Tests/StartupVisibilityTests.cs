using T3.Application.Common.Rbac;
using T3.Domain.Identity;

namespace T3.Application.Tests;

/// <summary>
/// Alan düzeyi maskelemenin (KVKK) testleri. Satır yetkisi geçse bile hassas
/// alanların açılmaması buraya bağlı.
/// </summary>
public class StartupVisibilityTests
{
    private static readonly Guid StartupId = Guid.NewGuid();
    private static readonly Guid OtherStartupId = Guid.NewGuid();

    [Fact]
    public void SuperAdmin_her_alani_gorur()
    {
        var visibility = StartupVisibility.For(FakeCurrentUser.As(UserRole.SuperAdmin), StartupId);

        Assert.Equal(StartupVisibility.All, visibility);
    }

    [Fact]
    public void ProgramYoneticisi_vergi_numarasi_disinda_her_alani_gorur()
    {
        var visibility = StartupVisibility.For(FakeCurrentUser.As(UserRole.ProgramManager), StartupId);

        Assert.False(visibility.ShowTaxNumber);
        Assert.True(visibility.ShowContactDetails);
        Assert.True(visibility.ShowTeamPersonalData);
        Assert.True(visibility.ShowExactAmounts);
        Assert.True(visibility.ShowDocuments);
    }

    [Fact]
    public void KararVerici_hicbir_hassas_alani_gormez()
    {
        var visibility = StartupVisibility.For(FakeCurrentUser.As(UserRole.DecisionMaker), StartupId);

        Assert.Equal(StartupVisibility.None, visibility);
    }

    [Fact]
    public void GirisimKullanicisi_kendi_verisinin_tamamini_gorur()
    {
        var visibility = StartupVisibility.For(
            new FakeCurrentUser { Role = UserRole.StartupUser, StartupId = StartupId },
            StartupId);

        Assert.Equal(StartupVisibility.All, visibility);
    }

    [Fact]
    public void GirisimKullanicisi_baska_girisimin_hicbir_alanini_gormez()
    {
        var visibility = StartupVisibility.For(
            new FakeCurrentUser { Role = UserRole.StartupUser, StartupId = OtherStartupId },
            StartupId);

        Assert.Equal(StartupVisibility.None, visibility);
    }

    [Fact]
    public void Kimlik_dogrulanmamis_istek_hicbir_alani_gormez()
    {
        var visibility = StartupVisibility.For(FakeCurrentUser.Anonymous, StartupId);

        Assert.Equal(StartupVisibility.None, visibility);
    }
}
