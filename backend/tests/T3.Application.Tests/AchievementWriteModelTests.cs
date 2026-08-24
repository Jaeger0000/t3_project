using T3.Application.Common.Rbac;
using T3.Application.Features.Achievements;
using T3.Domain.Achievements;
using T3.Domain.Identity;

namespace T3.Application.Tests;

/// <summary>
/// Başarı/finans kaydının tür-alan bağı (MVP #4).
///
/// Brief "serbest metin değil, alan bazlı veri modeli" istiyor: tutarsız bir
/// kayıt (tutarsız yatırım turu, mali yılı olmayan ciro) kaydedilebilirse
/// ekosistem raporları sessizce yanlış toplar.
/// </summary>
public class AchievementWriteModelTests
{
    private static readonly AchievementWriteModelValidator Validator = new();

    private static AchievementWriteModel Model(
        AchievementKind kind,
        decimal? amount = null,
        string? currency = "TRY",
        int? fiscalYear = null,
        int? quarter = null,
        InvestmentRoundType? roundType = null,
        GrantInstitution? institution = null,
        string? awardName = null,
        DateOnly? occurredOn = null) =>
        new(kind, occurredOn ?? new DateOnly(2025, 3, 26), null,
            amount, currency, fiscalYear, quarter,
            roundType, null, null, institution, null,
            awardName, null, null, null);

    private static bool IsValid(AchievementWriteModel model) =>
        Validator.Validate(model).IsValid;

    [Fact]
    public void Yatirim_turu_tutar_ve_tur_ister()
    {
        Assert.True(IsValid(Model(AchievementKind.Investment,
            amount: 12_000_000m, roundType: InvestmentRoundType.Seed)));

        Assert.False(IsValid(Model(AchievementKind.Investment,
            amount: null, roundType: InvestmentRoundType.Seed)));

        Assert.False(IsValid(Model(AchievementKind.Investment,
            amount: 12_000_000m, roundType: null)));
    }

    [Fact]
    public void Ciro_kaydi_mali_yil_ister()
    {
        Assert.True(IsValid(Model(AchievementKind.Revenue,
            amount: 2_800_000m, fiscalYear: 2024)));

        Assert.False(IsValid(Model(AchievementKind.Revenue,
            amount: 2_800_000m, fiscalYear: null)));
    }

    [Fact]
    public void Ceyrek_bir_ile_dort_arasinda_olmali()
    {
        Assert.True(IsValid(Model(AchievementKind.Revenue,
            amount: 1m, fiscalYear: 2024, quarter: 4)));

        Assert.False(IsValid(Model(AchievementKind.Revenue,
            amount: 1m, fiscalYear: 2024, quarter: 5)));
    }

    [Fact]
    public void Odul_kaydi_tutar_istemez_ad_ister()
    {
        Assert.True(IsValid(Model(AchievementKind.Award, awardName: "TEKNOFEST Birinciliği")));
        Assert.False(IsValid(Model(AchievementKind.Award, awardName: null)));
    }

    [Fact]
    public void Hibe_kaydi_kurum_ister()
    {
        Assert.True(IsValid(Model(AchievementKind.Grant,
            amount: 750_000m, institution: GrantInstitution.Tubitak)));

        Assert.False(IsValid(Model(AchievementKind.Grant,
            amount: 750_000m, institution: null)));
    }

    /// <summary>
    /// Para birimi serbest metin olamaz: agregatlar yalnızca aynı birimi
    /// toplayabiliyor, "TL" ile "TRY" karışırsa toplam sessizce bozulur.
    /// </summary>
    [Fact]
    public void Para_birimi_uc_harfli_kod_olmali()
    {
        Assert.True(IsValid(Model(AchievementKind.Grant, amount: 1m, currency: "USD",
            institution: GrantInstitution.Kosgeb)));

        Assert.False(IsValid(Model(AchievementKind.Grant, amount: 1m, currency: "TL",
            institution: GrantInstitution.Kosgeb)));
    }

    [Fact]
    public void Gelecek_tarihli_kayit_kabul_edilmez()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        Assert.False(IsValid(Model(AchievementKind.Award,
            awardName: "Ödül", occurredOn: tomorrow)));
    }

    /// <summary>
    /// Birim verilmediğinde rapor birimi varsayılır; boş bırakılan kayıt
    /// agregattan düşerdi.
    /// </summary>
    [Fact]
    public void Para_birimi_bos_birakilirsa_rapor_birimi_yazilir()
    {
        var entity = AchievementKinds.NewFor(AchievementKind.Grant, Guid.NewGuid());
        Model(AchievementKind.Grant, amount: 500m, currency: null,
            institution: GrantInstitution.Kosgeb).ApplyTo(entity);

        Assert.Equal("TRY", Assert.IsType<GrantRecord>(entity).Currency);
    }

    [Fact]
    public void Tur_ile_varlik_sinifi_ortusuyor()
    {
        foreach (var kind in Enum.GetValues<AchievementKind>())
        {
            var entity = AchievementKinds.NewFor(kind, Guid.NewGuid());
            Assert.Equal(kind, AchievementKinds.Of(entity));
        }
    }

    /// <summary>
    /// Karar Verici satırı görür, meblağı görmez. <c>AmountMasked</c> olmadan
    /// arayüz "tutar girilmemiş" ile "yetkiniz yok" ayrımını yapamaz — Faz 2'de
    /// bu ayrım yokken maskelenen tutar ekranda <c>0 ₺</c> görünmüştü.
    /// </summary>
    [Fact]
    public void Tutar_yetkisi_yoksa_meblag_maskelenir_satir_kalir()
    {
        var startupId = Guid.NewGuid();
        var round = new InvestmentRound
        {
            StartupId = startupId,
            OccurredOn = new DateOnly(2025, 3, 26),
            RoundType = InvestmentRoundType.Seed,
            Amount = 12_000_000m,
            Valuation = 120_000_000m,
            Currency = "TRY"
        };

        var masked = round.ToResponse(
            StartupVisibility.For(FakeCurrentUser.As(UserRole.DecisionMaker), startupId));

        Assert.True(masked.AmountMasked);
        Assert.Null(masked.Amount);
        Assert.Null(masked.Valuation);
        Assert.Equal("Seed turu", masked.Title);

        var visible = round.ToResponse(StartupVisibility.All);

        Assert.False(visible.AmountMasked);
        Assert.Equal(12_000_000m, visible.Amount);
        Assert.Equal(120_000_000m, visible.Valuation);
    }

    /// <summary>Tutarsız kayıt türlerinde maskeleme bayrağı yanlış yere düşmemeli.</summary>
    [Fact]
    public void Tutarsiz_kayitta_maskeleme_bayragi_yanlis_kalkmaz()
    {
        var award = new AwardRecord
        {
            StartupId = Guid.NewGuid(),
            OccurredOn = new DateOnly(2024, 9, 1),
            Name = "TEKNOFEST Birinciliği",
            Rank = 1
        };

        var response = award.ToResponse(
            StartupVisibility.For(FakeCurrentUser.As(UserRole.DecisionMaker), Guid.NewGuid()));

        Assert.False(response.AmountMasked);
        Assert.Null(response.Amount);
        Assert.Equal("TEKNOFEST Birinciliği — 1. sıra", response.Title);
    }
}
