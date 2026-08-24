using T3.Application.Features.Achievements;
using T3.Application.Features.Approvals.SubmitChangeRequest;
using T3.Application.Features.Startups;
using T3.Domain.Achievements;
using T3.Application.Features.Startups.Team;
using T3.Domain.Approvals;
using T3.Domain.Startups;

namespace T3.Application.Tests;

/// <summary>
/// Gövde bileşimlerinin doğrulaması. Bu doğrulayıcı güvenlik sınırının
/// parçası: hedef türü, işlem ve gövde üçlüsünün geçersiz bileşimleri kuyruğa
/// girerse onay anında ne uygulanacağı belirsiz kalır.
/// </summary>
public class SubmitChangeRequestValidatorTests
{
    private static readonly SubmitChangeRequestValidator Validator = new();

    private static readonly StartupWriteModel ValidStartup = new(
        "Anadolu Robotik", null, null, null, Sector.Defense,
        ["Robotik"], null, null, null, "Ankara", null, null, StartupStatus.Active);

    private static readonly TeamMemberWriteModel ValidMember = new(
        "Elif Yıldırım", "CTO", "elif@ornek.test", null, null, true, null);

    private static readonly AchievementWriteModel ValidAchievement = new(
        Kind: AchievementKind.Investment,
        OccurredOn: new DateOnly(2025, 3, 26),
        Note: null,
        Amount: 12_000_000m,
        Currency: "TRY",
        FiscalYear: null,
        Quarter: null,
        RoundType: InvestmentRoundType.Seed,
        Valuation: 120_000_000m,
        InvestorNames: ["T3 Girişim Fonu"],
        Institution: null,
        ProgramName: null,
        AwardName: null,
        Organization: null,
        Rank: null,
        TargetCountries: null);

    private static bool IsValid(SubmitChangeRequestRequest request) =>
        Validator.Validate(request).IsValid;

    [Fact]
    public void Girisim_profili_guncelleme_onerisi_gecerlidir()
    {
        Assert.True(IsValid(new SubmitChangeRequestRequest(
            ChangeTargetType.Startup, ChangeOperation.Update, null, ValidStartup, null, null)));
    }

    /// <summary>
    /// Girişimin kendi kaydını silme önerisi gönderememesi kasıtlı: silme
    /// zincirleme sonuçları olan bir işlem, onay akışının dışında tutuluyor.
    /// </summary>
    [Theory]
    [InlineData(ChangeOperation.Create)]
    [InlineData(ChangeOperation.Delete)]
    public void Girisim_kaydi_icin_yalnizca_guncelleme_kabul_edilir(ChangeOperation operation)
    {
        Assert.False(IsValid(new SubmitChangeRequestRequest(
            ChangeTargetType.Startup, operation, null, ValidStartup, null, null)));
    }

    [Fact]
    public void Girisim_onerisinde_targetId_gonderilemez()
    {
        Assert.False(IsValid(new SubmitChangeRequestRequest(
            ChangeTargetType.Startup, ChangeOperation.Update, Guid.NewGuid(), ValidStartup, null, null)));
    }

    [Fact]
    public void Girisim_onerisinde_govde_zorunludur()
    {
        Assert.False(IsValid(new SubmitChangeRequestRequest(
            ChangeTargetType.Startup, ChangeOperation.Update, null, null, null, null)));
    }

    [Fact]
    public void Yeni_ekip_uyesi_onerisinde_targetId_gonderilmez()
    {
        Assert.True(IsValid(new SubmitChangeRequestRequest(
            ChangeTargetType.TeamMember, ChangeOperation.Create, null, null, ValidMember, null)));

        Assert.False(IsValid(new SubmitChangeRequestRequest(
            ChangeTargetType.TeamMember, ChangeOperation.Create,
            Guid.NewGuid(), null, ValidMember, null)));
    }

    [Fact]
    public void Ekip_uyesi_guncellemesinde_targetId_zorunludur()
    {
        Assert.False(IsValid(new SubmitChangeRequestRequest(
            ChangeTargetType.TeamMember, ChangeOperation.Update, null, null, ValidMember, null)));

        Assert.True(IsValid(new SubmitChangeRequestRequest(
            ChangeTargetType.TeamMember, ChangeOperation.Update,
            Guid.NewGuid(), null, ValidMember, null)));
    }

    /// <summary>Silme önerisinde taşınacak gövde yok, hedef kimliği yeterli.</summary>
    [Fact]
    public void Ekip_uyesi_silme_onerisi_govdesiz_gecerlidir()
    {
        Assert.True(IsValid(new SubmitChangeRequestRequest(
            ChangeTargetType.TeamMember, ChangeOperation.Delete, Guid.NewGuid(), null, null, null)));
    }

    // --- Başarı/finans kayıtları (Faz 4) ---------------------------------

    [Fact]
    public void Basari_kaydi_ekleme_onerisi_gecerlidir()
    {
        Assert.True(IsValid(new SubmitChangeRequestRequest(
            ChangeTargetType.Achievement, ChangeOperation.Create, null, null, null,
            ValidAchievement)));
    }

    [Fact]
    public void Basari_kaydi_onerisinde_govde_zorunludur()
    {
        Assert.False(IsValid(new SubmitChangeRequestRequest(
            ChangeTargetType.Achievement, ChangeOperation.Create, null, null, null, null)));
    }

    [Theory]
    [InlineData(ChangeOperation.Update)]
    [InlineData(ChangeOperation.Delete)]
    public void Basari_kaydi_guncelleme_ve_silmede_targetId_zorunludur(ChangeOperation operation)
    {
        Assert.False(IsValid(new SubmitChangeRequestRequest(
            ChangeTargetType.Achievement, operation, null, null, null,
            operation == ChangeOperation.Delete ? null : ValidAchievement)));
    }

    [Fact]
    public void Yeni_basari_kaydi_onerisinde_targetId_gonderilemez()
    {
        Assert.False(IsValid(new SubmitChangeRequestRequest(
            ChangeTargetType.Achievement, ChangeOperation.Create, Guid.NewGuid(), null, null,
            ValidAchievement)));
    }

    /// <summary>
    /// Tür ile alanların bağı gönderim anında da zorunlu: tutarsız bir yatırım
    /// turu kuyruğa girerse onay anında ne kaydedileceği belirsiz kalır.
    /// </summary>
    [Fact]
    public void Basari_kaydi_govdesi_dogrudan_yazma_kurallariyla_dogrulanir()
    {
        var invalid = ValidAchievement with { Amount = null, RoundType = null };

        Assert.False(IsValid(new SubmitChangeRequestRequest(
            ChangeTargetType.Achievement, ChangeOperation.Create, null, null, null, invalid)));
    }

    // --- Dokümanlar (Faz 4) ----------------------------------------------

    /// <summary>
    /// Doküman yüklemesi JSON gövdesiyle gönderilemez: dosya multipart olarak
    /// doküman ucuna gider ve öneri orada üretilir. Buradan gelen bir "Create"
    /// isteği dosyasız bir doküman kaydı önerirdi.
    /// </summary>
    [Theory]
    [InlineData(ChangeOperation.Create)]
    [InlineData(ChangeOperation.Update)]
    public void Dokuman_yuklemesi_bu_uctan_gonderilemez(ChangeOperation operation)
    {
        Assert.False(IsValid(new SubmitChangeRequestRequest(
            ChangeTargetType.Document, operation, Guid.NewGuid(), null, null, null)));
    }

    [Fact]
    public void Dokuman_kaldirma_onerisi_gecerlidir()
    {
        Assert.True(IsValid(new SubmitChangeRequestRequest(
            ChangeTargetType.Document, ChangeOperation.Delete, Guid.NewGuid(), null, null, null)));
    }

    [Fact]
    public void Dokuman_kaldirma_onerisinde_targetId_zorunludur()
    {
        Assert.False(IsValid(new SubmitChangeRequestRequest(
            ChangeTargetType.Document, ChangeOperation.Delete, null, null, null, null)));
    }

    /// <summary>
    /// Onay akışı doğrudan yazmadan gevşek olamaz: geçersiz gövde onay
    /// beklerken doğrulamayı atlamış olurdu.
    /// </summary>
    [Fact]
    public void Govde_dogrudan_yazma_kurallariyla_dogrulanir()
    {
        var invalid = ValidStartup with { Name = "", TaxNumber = "12" };

        Assert.False(IsValid(new SubmitChangeRequestRequest(
            ChangeTargetType.Startup, ChangeOperation.Update, null, invalid, null, null)));
    }

    [Fact]
    public void Ekip_uyesi_govdesi_de_dogrudan_yazma_kurallariyla_dogrulanir()
    {
        var invalid = ValidMember with { Email = "gecersiz-eposta" };

        Assert.False(IsValid(new SubmitChangeRequestRequest(
            ChangeTargetType.TeamMember, ChangeOperation.Create, null, null, invalid, null)));
    }
}
