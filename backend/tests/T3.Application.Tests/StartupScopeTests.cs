using T3.Application.Common.Rbac;
using T3.Domain.Identity;
using T3.Domain.Programs;
using T3.Domain.Startups;

namespace T3.Application.Tests;

/// <summary>
/// Satır düzeyi yetkilendirmenin testleri. Bu sınıf sistemdeki en kritik
/// güvenlik sınırı: her girişim sorgusu buradan geçiyor, bir hata bütün
/// rollere yanlış veri açar.
/// </summary>
public class StartupScopeTests
{
    private static readonly Guid IncubationProgramId = Guid.NewGuid();
    private static readonly Guid FestivalProgramId = Guid.NewGuid();

    private static readonly Startup OwnStartup = WithProgram("Kendi Girişimi", IncubationProgramId);
    private static readonly Startup IncubationStartup = WithProgram("Kuluçka Girişimi", IncubationProgramId);
    private static readonly Startup FestivalStartup = WithProgram("Festival Girişimi", FestivalProgramId);
    private static readonly Startup OrphanStartup = new() { Name = "Programsız Girişim" };

    private static readonly Startup[] AllStartups =
        [OwnStartup, IncubationStartup, FestivalStartup, OrphanStartup];

    private static Startup WithProgram(string name, Guid programId) => new()
    {
        Name = name,
        Participations =
        [
            new ProgramParticipation
            {
                ProgramTerm = new ProgramTerm { ProgramId = programId, Name = "Dönem" }
            }
        ]
    };

    [Fact]
    public void SuperAdmin_tum_girisimleri_gorur()
    {
        var names = Apply(FakeCurrentUser.As(UserRole.SuperAdmin));

        Assert.Equal(4, names.Length);
    }

    [Fact]
    public void KararVerici_tum_satirlari_gorur_kisit_alan_duzeyindedir()
    {
        var names = Apply(FakeCurrentUser.As(UserRole.DecisionMaker));

        Assert.Equal(4, names.Length);
    }

    [Fact]
    public void GirisimKullanicisi_yalnizca_kendi_girisimini_gorur()
    {
        var names = Apply(new FakeCurrentUser
        {
            Role = UserRole.StartupUser,
            StartupId = OwnStartup.Id
        });

        Assert.Equal(["Kendi Girişimi"], names);
    }

    [Fact]
    public void GirisimKullanicisi_girisimi_yoksa_hicbir_sey_gormez()
    {
        var names = Apply(new FakeCurrentUser { Role = UserRole.StartupUser, StartupId = null });

        Assert.Empty(names);
    }

    [Fact]
    public void ProgramYoneticisi_yalnizca_atandigi_programlardan_gecen_girisimleri_gorur()
    {
        var names = Apply(new FakeCurrentUser
        {
            Role = UserRole.ProgramManager,
            AssignedProgramIds = [IncubationProgramId]
        });

        Assert.Equal(["Kendi Girişimi", "Kuluçka Girişimi"], names.Order().ToArray());
    }

    [Fact]
    public void ProgramYoneticisi_atamasi_yoksa_hicbir_sey_gormez()
    {
        var names = Apply(new FakeCurrentUser
        {
            Role = UserRole.ProgramManager,
            AssignedProgramIds = []
        });

        Assert.Empty(names);
    }

    [Fact]
    public void Kimlik_dogrulanmamis_istek_hicbir_sey_gormez()
    {
        var names = Apply(FakeCurrentUser.Anonymous);

        Assert.Empty(names);
    }

    [Fact]
    public void Rolu_olmayan_kimlik_hicbir_sey_gormez()
    {
        var names = Apply(new FakeCurrentUser { Role = null });

        Assert.Empty(names);
    }

    [Theory]
    [InlineData(UserRole.SuperAdmin, true)]
    [InlineData(UserRole.ProgramManager, true)]
    [InlineData(UserRole.StartupUser, false)]
    [InlineData(UserRole.DecisionMaker, false)]
    public void Dogrudan_duzenleme_yetkisi_role_bagli(UserRole role, bool expected)
    {
        var scope = new StartupScope(FakeCurrentUser.As(role));

        Assert.Equal(expected, scope.CanEditDirectly(OwnStartup.Id));
        Assert.Equal(expected, scope.CanCreateStartups);
        Assert.Equal(expected, scope.CanReviewApprovals);
    }

    private static string[] Apply(FakeCurrentUser user) =>
        new StartupScope(user)
            .Apply(AllStartups.AsQueryable())
            .Select(s => s.Name)
            .ToArray();
}
