using T3.Application.Features.Approvals;
using T3.Application.Features.Startups;
using T3.Domain.Startups;

namespace T3.Application.Tests;

/// <summary>
/// Öneri gövdesinin serileştirme davranışı. Gönderim ile onay arasında günler
/// geçebildiği için bu ayarın kayması, kuyrukta okunamaz öneri bırakır.
/// </summary>
public class ChangeRequestJsonTests
{
    private static readonly StartupWriteModel Model = new(
        "Anadolu Robotik", null, "1234567890", new DateOnly(2021, 5, 4),
        Sector.Defense, ["Robotik"], null, null, null, "Ankara",
        null, null, StartupStatus.Active);

    /// <summary>
    /// "Alan boşaltıldı" ile "alan gönderilmedi" ayrımı diff'in temeli; null
    /// atlanırsa iki durum aynı görünür ve boşaltma sessizce kaybolur.
    /// </summary>
    [Fact]
    public void Null_alanlar_atlanmaz()
    {
        var json = ChangeRequestJson.Serialize(Model);

        Assert.Contains("\"legalName\":null", json);
    }

    /// <summary>Enum ad olarak yazılır: jsonb kaydı denetim izinde elle okunabilir kalıyor.</summary>
    [Fact]
    public void Enumlar_ad_olarak_yazilir()
    {
        var json = ChangeRequestJson.Serialize(Model);

        Assert.Contains("\"sector\":\"Defense\"", json);
        Assert.Contains("\"status\":\"Active\"", json);
    }

    [Fact]
    public void Yazilan_govde_geri_okunabilir()
    {
        var roundTrip = ChangeRequestJson.TryDeserialize<StartupWriteModel>(
            ChangeRequestJson.Serialize(Model));

        Assert.NotNull(roundTrip);

        // Koleksiyon ayrı karşılaştırılıyor: record eşitliği listeyi referansla
        // kıyaslar, aynı içerikli iki liste eşit sayılmaz.
        Assert.Equal(Model.TechnologyAreas, roundTrip.TechnologyAreas);
        Assert.Equal(
            Model with { TechnologyAreas = null },
            roundTrip with { TechnologyAreas = null });
    }

    [Fact]
    public void Bozuk_govde_istisna_firlatmaz()
    {
        Assert.Null(ChangeRequestJson.TryDeserialize<StartupWriteModel>("{bozuk"));
        Assert.Null(ChangeRequestJson.TryDeserialize<StartupWriteModel>(null));
        Assert.Null(ChangeRequestJson.TryDeserialize<StartupWriteModel>("   "));
    }

    /// <summary>
    /// Okunamayan gövde diff üretmiyor; çağıran taraf bunu "uygulanamaz öneri"
    /// olarak raporluyor, boş diff gösterip onaya izin vermiyor.
    /// </summary>
    [Fact]
    public void Okunamayan_govde_bos_diff_uretir()
    {
        var body = new ChangeRequestBody(
            Domain.Approvals.ChangeTargetType.Startup,
            Domain.Approvals.ChangeOperation.Update,
            PayloadJson: "{bozuk",
            BeforeJson: null);

        Assert.Empty(ChangeRequestBodies.Diff(body, Common.Rbac.StartupVisibility.All));
    }
}
