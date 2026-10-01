using T3.Application.Features.Achievements;
using T3.Application.Features.Achievements.ListAchievements;
using T3.Application.Features.Reports.GenerateStartupReport;
using T3.Application.Features.Startups.GetStartupCard;
using T3.Application.Features.Startups.GetStartupTimeline;
using T3.Domain.Achievements;
using T3.Domain.Programs;
using T3.Domain.Startups;

namespace T3.Application.Tests;

/// <summary>
/// AI raporunun dili. 1 Ekim 2026'da canlıda üretilen bir rapor şu cümleyi
/// içeriyordu: "En görünür boşluk, logoUrl alanının null olması; ... Ayrıca
/// visibility bloğunda taxNumber, teamPersonalData ... true". Yöneticiye
/// giden bir belgede veritabanı alan adı bulunamaz.
///
/// Üç savunma katmanı burada test ediliyor: (1) modele giden brif alan adı
/// taşımıyor, (2) eksik bilgi iş diliyle anlatılıyor, (3) model yine de
/// teknik yazarsa denetim yakalıyor.
/// </summary>
public class RaporDiliTests
{
    /// <summary>Canlıda basılan metnin kendisi — denetim bunu yakalamalı.</summary>
    private const string CanliSizintiMetni =
        "Eksik ve tamamlanmamış veri alanları. En görünür boşluk, logoUrl alanının "
        + "null olması; girişimin vitrinde görünürlüğü için bu alanın doldurulması "
        + "gerekiyor. İkinci olarak achievements.totalExport null geliyor.";

    [Fact]
    public void Canlida_basilan_teknik_cumle_yakalanir()
    {
        Assert.True(ReportProse.HasTechnicalLeak(CanliSizintiMetni));
    }

    [Theory]
    [InlineData("isVerified alanı boş geliyor.")]
    [InlineData("visibility bloğunda taxNumber değeri true.")]
    [InlineData("Profil JSON verisinde ürün açıklaması bulunmuyor.")]
    [InlineData("Logo bilgisi `null` olarak dönüyor.")]
    public void Teknik_ifadeler_yakalanir(string metin)
    {
        Assert.True(ReportProse.HasTechnicalLeak(metin));
    }

    [Theory]
    [InlineData("Girişimin logosu sisteme henüz yüklenmemiş; vitrinde görünürlüğü artırmak için eklenmesi öneriliyor.")]
    [InlineData("Şirket 2021 yılında Bursa'da kuruldu ve savunma alanında faaliyet gösteriyor.")]
    [InlineData("İhracat kaydı girilmemiş, bu yüzden ihracat performansı hakkında bir değerlendirme yapılamıyor.")]
    [InlineData("- Üç yatırım turu tamamlandı ve tutarların tamamı yetkili tarafından doğrulandı.")]
    public void Temiz_is_dili_yakalanmaz(string metin)
    {
        Assert.False(ReportProse.HasTechnicalLeak(metin));
    }

    /// <summary>
    /// Denetim, kanıt paketinde geçen kelimeyi sızıntı saymıyor: brifte alan
    /// adı yok, dolayısıyla orada da görünen bir kelime şemadan değil gerçek
    /// veriden geliyor (ürün adı, yatırımcı adı). Aksi hâlde denetim sağlam
    /// cümleleri atardı.
    /// </summary>
    [Fact]
    public void Veriden_gelen_kelime_sizinti_sayilmaz()
    {
        const string cumle = "Girişimin eTicaret platformu 2024'te devreye alındı.";

        Assert.True(ReportProse.HasTechnicalLeak(cumle));
        Assert.False(ReportProse.HasTechnicalLeak(
            cumle, evidence: "Ürün ve faaliyet açıklaması: eTicaret platformu"));
    }

    [Fact]
    public void Sadece_sizdiran_cumle_atilir_kalan_metin_durur()
    {
        const string metin =
            "Girişim 2021'de kuruldu ve savunma sanayinde faaliyet gösteriyor. "
            + "En görünür boşluk, logoUrl alanının null olması. "
            + "Ekip beş kişiye ulaştı ve iki yatırım turu tamamlandı.";

        var temiz = ReportProse.StripTechnicalSentences(metin);

        Assert.DoesNotContain("logoUrl", temiz);
        Assert.DoesNotContain("null", temiz);
        Assert.Contains("savunma sanayinde", temiz);
        Assert.Contains("iki yatırım turu tamamlandı", temiz);
    }

    [Fact]
    public void Sizdiran_madde_imi_tumuyle_atilir()
    {
        const string metin =
            "Öneriler aşağıda sıralanmıştır.\n"
            + "- logoUrl alanı doldurulmalı.\n"
            + "- Girişimin web sitesi profile eklenmeli.";

        var temiz = ReportProse.StripTechnicalSentences(metin);

        // Yarısı atılmış bir madde imi okunmaz: satır tek birim.
        Assert.DoesNotContain("logoUrl", temiz);
        Assert.DoesNotContain("doldurulmalı", temiz);
        Assert.Contains("- Girişimin web sitesi profile eklenmeli.", temiz);
    }

    [Fact]
    public void Brif_alan_adi_ya_da_null_icermez()
    {
        var brif = ReportEvidence.Build(Kart(), Cizelge(), Kayitlar());

        Assert.DoesNotContain("logoUrl", brif);
        Assert.DoesNotContain("isVerified", brif);
        Assert.DoesNotContain("totalExport", brif);
        Assert.DoesNotContain("visibility", brif, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("null", brif);
        Assert.DoesNotContain("{", brif);

        // Brifin kendisi de denetimden geçiyor: modele teknik kelime veren
        // bir girdi, modelin teknik yazmasının en kısa yolu.
        Assert.False(ReportProse.HasTechnicalLeak(brif));
    }

    /// <summary>
    /// Beyaz liste kontrolü: kişisel veri brife hiç yazılmıyor. AiRedaction
    /// alan adı kara listesiyle çalışıyordu, bu yol DTO'ya eklenen yeni bir
    /// hassas alanı da kapsıyor (bkz. G-04).
    /// </summary>
    [Fact]
    public void Brif_kisisel_veri_tasimaz()
    {
        var brif = ReportEvidence.Build(Kart(), Cizelge(), Kayitlar());

        Assert.DoesNotContain("Ayşe Yılmaz", brif);
        Assert.DoesNotContain("ayse@ornek.test", brif);
        Assert.DoesNotContain("05551112233", brif);
        Assert.DoesNotContain("1234567890", brif);
        Assert.DoesNotContain("linkedin", brif, StringComparison.OrdinalIgnoreCase);

        // Unvan ve katılım tarihi kalıyor: ekip bölümünün yazılabilmesi için
        // gereken, kişiyi tanımlamayan bilgi.
        Assert.Contains("Kurucu", brif);
    }

    [Fact]
    public void Dogrulama_durumu_insan_diliyle_yazilir()
    {
        var brif = ReportEvidence.Build(Kart(), Cizelge(), Kayitlar());

        Assert.Contains("yetkili onayı bekliyor", brif);
        Assert.DoesNotContain("IsVerified", brif, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Eksik_bilgiler_is_diliyle_listelenir()
    {
        var eksikler = ReportEvidence.Gaps(Kart(), Kayitlar());

        Assert.Contains("Logo görseli yüklenmemiş", eksikler);
        Assert.Contains("İhracat kaydı girilmemiş", eksikler);
        Assert.Contains("Hiç doküman yüklenmemiş", eksikler);
        Assert.All(eksikler, e => Assert.False(ReportProse.HasTechnicalLeak(e)));
    }

    /// <summary>
    /// "Veri yok" ile "görme yetkin yok" ayrımı: Program Yöneticisi vergi
    /// numarasını göremez, alan <c>null</c> gelir. Bunu "girilmemiş" diye
    /// raporlamak yanlış bilgi olurdu — aynı ayrım arayüzde de var
    /// (kilit simgesi ile boş alan farkı).
    /// </summary>
    [Fact]
    public void Maskeli_alan_eksik_bilgi_olarak_raporlanmaz()
    {
        var yetkisiz = Kart() with
        {
            TaxNumber = null,
            Visibility = new CardVisibilityResponse(
                ContactDetails: true, TaxNumber: false, ExactAmounts: true,
                TeamPersonalData: true, Documents: true)
        };

        Assert.DoesNotContain("Vergi numarası girilmemiş", ReportEvidence.Gaps(yetkisiz, Kayitlar()));

        var yetkili = Kart() with
        {
            TaxNumber = null,
            Visibility = new CardVisibilityResponse(
                ContactDetails: true, TaxNumber: true, ExactAmounts: true,
                TeamPersonalData: true, Documents: true)
        };

        Assert.Contains("Vergi numarası girilmemiş", ReportEvidence.Gaps(yetkili, Kayitlar()));
    }

    [Fact]
    public void Tutar_gizliyken_tutar_brife_yazilmaz()
    {
        var gizli = Kart() with
        {
            Visibility = new CardVisibilityResponse(
                ContactDetails: false, TaxNumber: false, ExactAmounts: false,
                TeamPersonalData: false, Documents: false)
        };

        var brif = ReportEvidence.Build(gizli, Cizelge(), Kayitlar(tutarMaskeli: true));

        Assert.DoesNotContain("7.500.000", brif);
        Assert.Contains("tutar bu raporda gizli tutuluyor", brif);
    }

    private static StartupCardResponse Kart() => new(
        Id: Guid.NewGuid(),
        Name: "Vektör Savunma Teknolojileri A.Ş.",
        LegalName: "Vektör Savunma Teknolojileri Anonim Şirketi",
        TaxNumber: "1234567890",
        FoundedOn: new DateOnly(2021, 3, 12),
        Sector: Sector.Defense,
        TechnologyAreas: ["Gömülü yazılım", "Sensör füzyonu"],
        ProductDescription: "İnsansız sistemler için sensör füzyonu yazılımı.",
        Website: null,
        LogoUrl: null,
        City: "Bursa",
        ContactEmail: "ayse@ornek.test",
        ContactPhone: "05551112233",
        Status: StartupStatus.Active,
        Team:
        [
            new CardTeamMemberResponse(
                Guid.NewGuid(), "Ayşe Yılmaz", "Genel Müdür", "ayse@ornek.test",
                "05551112233", "https://linkedin.com/in/ayse", IsFounder: true,
                JoinedOn: new DateOnly(2021, 3, 12))
        ],
        Programs:
        [
            new CardParticipationResponse(
                Guid.NewGuid(), Guid.NewGuid(), "T3 Hızlandırma Programı",
                ProgramType.Acceleration, "Savunma Koordinatörlüğü", Guid.NewGuid(),
                "2024 Güz", ParticipationStatus.Completed,
                new DateOnly(2024, 1, 14), new DateOnly(2024, 6, 20), Notes: null)
        ],
        Achievements: new CardAchievementSummaryResponse(
            TotalCount: 1, InvestmentRoundCount: 1, AwardCount: 0,
            TotalInvestment: 7_500_000m, TotalGrant: null,
            LatestAnnualRevenue: null, LatestRevenueYear: null,
            TotalExport: null, Currency: "TRY"),
        DocumentCount: 0,
        MilestoneCount: 1,
        CreatedAt: new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero),
        UpdatedAt: null,
        Visibility: new CardVisibilityResponse(
            ContactDetails: true, TaxNumber: true, ExactAmounts: true,
            TeamPersonalData: true, Documents: true));

    private static StartupTimelineResponse Cizelge() => new(
        StartupId: Guid.NewGuid(),
        StartupName: "Vektör Savunma Teknolojileri A.Ş.",
        ExactAmountsVisible: true,
        Entries:
        [
            new TimelineEntryResponse(
                TimelineEntryKind.Founding, new DateOnly(2021, 3, 12), "Girişim kuruldu",
                Description: null, Badge: null, Amount: null, Currency: null,
                IsVerified: true, SourceId: null)
        ]);

    private static AchievementListResponse Kayitlar(bool tutarMaskeli = false) => new(
        StartupId: Guid.NewGuid(),
        ExactAmountsVisible: !tutarMaskeli,
        Items:
        [
            new AchievementResponse(
                Id: Guid.NewGuid(),
                StartupId: Guid.NewGuid(),
                Kind: AchievementKind.Investment,
                KindLabel: "Yatırım",
                OccurredOn: new DateOnly(2024, 5, 4),
                Title: "Seed turu",
                Note: null,
                Amount: tutarMaskeli ? null : 7_500_000m,
                Currency: "TRY",
                AmountMasked: tutarMaskeli,
                FiscalYear: null,
                Quarter: null,
                PeriodLabel: null,
                RoundType: InvestmentRoundType.Seed,
                RoundTypeLabel: "Seed",
                Valuation: null,
                InvestorNames: [],
                Institution: null,
                InstitutionLabel: null,
                ProgramName: null,
                AwardName: null,
                Organization: null,
                Rank: null,
                TargetCountries: [],
                IsVerified: false,
                VerifiedAt: null,
                CreatedAt: new DateTimeOffset(2024, 5, 4, 0, 0, 0, TimeSpan.Zero),
                UpdatedAt: null)
        ]);
}
