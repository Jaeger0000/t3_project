using T3.Application.Common.Rbac;
using T3.Application.Features.Startups;
using T3.Domain.Identity;
using T3.Domain.Startups;

namespace T3.Application.Tests;

/// <summary>
/// Maskelemenin <b>yazma</b> yolundaki karşılığı. Maskeli alan istemciye null
/// gittiği için tam değiştirmeli PUT onu sessizce siliyordu: vergi numarasını
/// göremeyen Program Yöneticisi'nin her düzenlemesi numarayı boşaltırdı.
/// </summary>
public class StartupWriteMaskingTests
{
    private static Startup Existing() => new()
    {
        Name = "Anadolu Robotik",
        TaxNumber = "1234567890",
        ContactEmail = "iletisim@anadolurobotik.test",
        ContactPhone = "+90 555 000 00 00",
        Sector = Sector.Manufacturing,
        Status = StartupStatus.Active
    };

    private static StartupWriteModel Model(Startup s) => new(
        Name: s.Name,
        LegalName: null,
        // Arayüz maskeli alanı null gönderir; test tam olarak bu gövdeyi taklit ediyor.
        TaxNumber: null,
        FoundedOn: null,
        Sector: s.Sector,
        TechnologyAreas: null,
        ProductDescription: null,
        Website: null,
        LogoUrl: null,
        City: "Sivas",
        ContactEmail: null,
        ContactPhone: null,
        Status: s.Status);

    [Fact]
    public void Vergi_numarasini_goremeyen_rol_alani_bosaltamaz()
    {
        var startup = Existing();
        var visibility = StartupVisibility.For(
            FakeCurrentUser.As(UserRole.ProgramManager), startup.Id);

        Model(startup).ApplyTo(startup, visibility);

        Assert.Equal("1234567890", startup.TaxNumber);
        Assert.Equal("Sivas", startup.City);
    }

    [Fact]
    public void Iletisim_bilgisini_goremeyen_rol_alani_bosaltamaz()
    {
        var startup = Existing();

        Model(startup).ApplyTo(startup, StartupVisibility.None);

        Assert.Equal("iletisim@anadolurobotik.test", startup.ContactEmail);
        Assert.Equal("+90 555 000 00 00", startup.ContactPhone);
    }

    [Fact]
    public void Alani_goren_rol_bosaltabilir()
    {
        var startup = Existing();

        Model(startup).ApplyTo(startup, StartupVisibility.All);

        Assert.Null(startup.TaxNumber);
        Assert.Null(startup.ContactEmail);
        Assert.Null(startup.ContactPhone);
    }
}
