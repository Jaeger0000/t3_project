using System.Globalization;
using System.Text;
using T3.Application.Features.Achievements.ListAchievements;
using T3.Application.Features.Startups;
using T3.Application.Features.Startups.GetStartupCard;
using T3.Application.Features.Startups.GetStartupTimeline;

namespace T3.Application.Features.Reports.GenerateStartupReport;

/// <summary>
/// Modele gidecek kanıt paketini Türkçe, insan diliyle hazırlar.
///
/// Neden JSON değil: 1 Ekim 2026'da canlıda üretilen bir rapor şu cümleyi
/// içeriyordu — "En görünür boşluk, logoUrl alanının null olması". Kusur
/// modelde değil girdideydi. Kanıt paketi <c>JsonSerializer.Serialize(...)</c>
/// çıktısıydı; yani modelin "bu bilgi eksik" diyebilmek için elindeki tek
/// kelime dağarcığı camelCase alan adları ve <c>null</c> sabitiydi. Biçim
/// talimatı da üstüne "bir alan boş/maskeli geldiyse bunu açıkça söyle"
/// diyordu. İkisi birleşince rapor, veritabanı şemasını anlatmaya başladı.
///
/// Bu yüzden paket artık hiçbir alan adı taşımıyor: her değer Türkçe bir
/// etiketle yazılıyor, değeri yoksa satır hiç yazılmıyor. Eksikler ayrı bir
/// "kayıtlarda bulunmayan bilgiler" listesinde ve iş diliyle duruyor
/// ("logo görseli yüklenmemiş") — çünkü Gelişim Önerileri bölümünün somut
/// boşluklara ihtiyacı var; boşluk verilmediğinde model genel geçer tavsiye
/// uyduruyor.
///
/// Kişisel veri (ad, e-posta, telefon, LinkedIn, vergi numarası) buraya
/// hiç yazılmıyor. <see cref="Assistant.AiRedaction"/> alan adı kara
/// listesiyle çalışıyordu; bu paket ise beyaz liste — DTO'ya yeni bir hassas
/// alan eklendiğinde sessizce dışarı sızmasının yolu yok (bkz. G-04).
/// </summary>
public static class ReportEvidence
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    public static string Build(
        StartupCardResponse card,
        StartupTimelineResponse timeline,
        AchievementListResponse achievements)
    {
        var b = new StringBuilder();

        b.AppendLine("GİRİŞİM KÜNYESİ");
        Line(b, "Girişim adı", card.Name);
        Line(b, "Resmî unvan", card.LegalName);
        Line(b, "Sektör", StartupLabels.Sector(card.Sector));
        Line(b, "Şehir", card.City);
        Line(b, "Durum", StartupLabels.Status(card.Status));
        Line(b, "Kuruluş tarihi", Date(card.FoundedOn));
        Line(b, "Çalıştığı teknoloji alanları",
            card.TechnologyAreas.Count > 0 ? string.Join(", ", card.TechnologyAreas) : null);
        Line(b, "Web sitesi", card.Website);
        Line(b, "Ürün ve faaliyet açıklaması", card.ProductDescription);
        Line(b, "Sisteme kayıt tarihi", Date(card.CreatedAt));
        Line(b, "Profilin son güncellenme tarihi", Date(card.UpdatedAt));

        b.AppendLine();
        b.AppendLine("KAYIT SAYILARI");
        Line(b, "Program katılımı", Count(card.Programs.Count));
        Line(b, "Ekip üyesi", Count(card.Team.Count));
        Line(b, "Başarı ve finans kaydı", Count(card.Achievements.TotalCount));
        Line(b, "Yüklenmiş doküman", Count(card.DocumentCount));
        Line(b, "Gelişim adımı (kilometre taşı)", Count(card.MilestoneCount));

        Finance(b, card);
        Team(b, card);
        Programs(b, card);
        Timeline(b, timeline);
        Achievements(b, achievements);

        var gaps = Gaps(card, achievements);
        if (gaps.Count > 0)
        {
            b.AppendLine();
            b.AppendLine("KAYITLARDA BULUNMAYAN BİLGİLER");
            b.AppendLine(
                "Aşağıdaki başlıklar sisteme henüz girilmemiş. Öneri yazarken "
                + "yalnızca bu listeye dayanabilirsin; eksikliği teknik bir "
                + "ifadeyle değil, buradaki iş diliyle anlat.");
            foreach (var gap in gaps)
                b.AppendLine($"- {gap}");
        }

        return b.ToString();
    }

    /// <summary>
    /// Eksik bilgilerin iş dilindeki listesi. Ayrı ve public: raporun en çok
    /// hata yapılan bölümü (Gelişim Önerileri) bu listeye dayanıyor, bu yüzden
    /// doğrudan test ediliyor.
    ///
    /// Maskeleme ile boşluk ayrımı burada korunuyor: tutarı ya da vergi
    /// numarasını görme yetkisi olmayan bir görüntüleyici için bu alanlar
    /// <c>null</c> geliyor — "girilmemiş" demek yanlış olurdu. Aynı ayrım
    /// arayüzde de var (kilit simgesi ile boş alan farkı).
    /// </summary>
    public static IReadOnlyList<string> Gaps(
        StartupCardResponse card, AchievementListResponse achievements)
    {
        var gaps = new List<string>();
        var ach = card.Achievements;

        if (string.IsNullOrWhiteSpace(card.LogoUrl)) gaps.Add("Logo görseli yüklenmemiş");
        if (string.IsNullOrWhiteSpace(card.Website)) gaps.Add("Web sitesi adresi girilmemiş");
        if (string.IsNullOrWhiteSpace(card.ProductDescription))
            gaps.Add("Ürün ve faaliyet açıklaması yazılmamış");
        if (string.IsNullOrWhiteSpace(card.LegalName)) gaps.Add("Resmî şirket unvanı girilmemiş");
        if (card.FoundedOn is null) gaps.Add("Kuruluş tarihi girilmemiş");
        if (string.IsNullOrWhiteSpace(card.City)) gaps.Add("Faaliyet gösterdiği şehir girilmemiş");
        if (card.TechnologyAreas.Count == 0) gaps.Add("Çalıştığı teknoloji alanları seçilmemiş");

        // Hassas alanlar yalnızca görme yetkisi varken "eksik" sayılabilir;
        // yetki yoksa veri olabilir de olmayabilir de, bilemiyoruz.
        if (card.Visibility.ContactDetails && string.IsNullOrWhiteSpace(card.ContactEmail))
            gaps.Add("İletişim e-posta adresi girilmemiş");
        if (card.Visibility.ContactDetails && string.IsNullOrWhiteSpace(card.ContactPhone))
            gaps.Add("İletişim telefonu girilmemiş");
        if (card.Visibility.TaxNumber && string.IsNullOrWhiteSpace(card.TaxNumber))
            gaps.Add("Vergi numarası girilmemiş");

        if (card.Team.Count == 0) gaps.Add("Hiç ekip üyesi kaydedilmemiş");
        if (card.Programs.Count == 0) gaps.Add("Hiçbir program katılımı kaydedilmemiş");
        if (card.DocumentCount == 0) gaps.Add("Hiç doküman yüklenmemiş");
        if (card.MilestoneCount == 0) gaps.Add("Hiç gelişim adımı (kilometre taşı) kaydedilmemiş");

        if (card.Visibility.ExactAmounts)
        {
            if (ach.TotalInvestment is null) gaps.Add("Alınmış yatırım kaydı yok");
            if (ach.TotalGrant is null) gaps.Add("Hibe ya da destek kaydı yok");
            if (ach.LatestAnnualRevenue is null) gaps.Add("Yıllık ciro kaydı girilmemiş");
            if (ach.TotalExport is null) gaps.Add("İhracat kaydı girilmemiş");
        }

        var unverified = achievements.Items.Count(a => !a.IsVerified);
        if (unverified > 0)
            gaps.Add($"{unverified} başarı/finans kaydı henüz yetkili tarafından doğrulanmamış");

        return gaps;
    }

    private static void Finance(StringBuilder b, StartupCardResponse card)
    {
        var ach = card.Achievements;

        b.AppendLine();
        b.AppendLine("FİNANS ÖZETİ");

        // Tutarlar yalnızca görme yetkisi varken yazılıyor. Kart zaten
        // maskeliyor (bkz. GetStartupCardHandler), buradaki ikinci kontrol
        // bilinçli: bu paket yurt dışındaki bir model sağlayıcısına gidiyor,
        // maskelemenin tek bir noktaya bağlı kalması istenmiyor.
        if (card.Visibility.ExactAmounts)
        {
            Line(b, "Toplam alınan yatırım", Money(ach.TotalInvestment, ach.Currency));
            Line(b, "Toplam hibe ve destek", Money(ach.TotalGrant, ach.Currency));
            Line(b, "En son yıllık ciro", ach.LatestAnnualRevenue is { } revenue
                ? $"{Money(revenue, ach.Currency)} ({ach.LatestRevenueYear} yılı)"
                : null);
            Line(b, "Toplam ihracat", Money(ach.TotalExport, ach.Currency));
        }
        else
        {
            b.AppendLine(
                "Bu raporu isteyen kullanıcının tutarları görme yetkisi yok, bu "
                + "yüzden tutarlar paylaşılmıyor. Finans bölümünde tutar yerine "
                + "kayıt sayılarından söz et ve tutarların gizli tutulduğunu belirt.");
        }
        Line(b, "Yatırım turu sayısı", Count(ach.InvestmentRoundCount));
        Line(b, "Ödül sayısı", Count(ach.AwardCount));
    }

    private static void Team(StringBuilder b, StartupCardResponse card)
    {
        if (card.Team.Count == 0) return;

        b.AppendLine();
        b.AppendLine($"EKİP ({card.Team.Count} kişi)");
        b.AppendLine(
            "Kişi adları ve iletişim bilgileri bu rapora dâhil edilmiyor "
            + "(kişisel verinin korunması). Ekibi unvan ve katılım tarihiyle "
            + "anlat, isim verilmediğini ayrıca söyleme.");

        foreach (var member in card.Team)
        {
            var parts = new List<string> { member.IsFounder ? "Kurucu" : "Ekip üyesi" };

            if (!string.IsNullOrWhiteSpace(member.Title)) parts.Add(member.Title!);
            if (Date(member.JoinedOn) is { } joined) parts.Add($"{joined} tarihinde katıldı");

            b.AppendLine($"- {string.Join(" · ", parts)}");
        }
    }

    private static void Programs(StringBuilder b, StartupCardResponse card)
    {
        if (card.Programs.Count == 0) return;

        b.AppendLine();
        b.AppendLine("PROGRAM GEÇMİŞİ");

        foreach (var p in card.Programs.OrderBy(p => p.JoinedOn))
        {
            var parts = new List<string>
            {
                $"{p.ProgramName} — {p.TermName} dönemi",
                TimelineLabels.Participation(p.Status),
                $"{Date(p.JoinedOn)} tarihinde katıldı"
            };

            if (Date(p.LeftOn) is { } left) parts.Add($"{left} tarihinde ayrıldı");
            if (!string.IsNullOrWhiteSpace(p.Coordinatorship))
                parts.Add($"{p.Coordinatorship} koordinatörlüğü");
            if (!string.IsNullOrWhiteSpace(p.Notes)) parts.Add($"not: {p.Notes}");

            b.AppendLine($"- {string.Join(" · ", parts)}");
        }
    }

    private static void Timeline(StringBuilder b, StartupTimelineResponse timeline)
    {
        if (timeline.Entries.Count == 0) return;

        b.AppendLine();
        b.AppendLine("GELİŞİM KRONOLOJİSİ (eskiden yeniye)");

        foreach (var entry in timeline.Entries.OrderBy(e => e.OccurredOn))
        {
            var parts = new List<string> { entry.Title };

            if (!string.IsNullOrWhiteSpace(entry.Description)) parts.Add(entry.Description!);
            if (!string.IsNullOrWhiteSpace(entry.Badge)) parts.Add(entry.Badge!);

            if (timeline.ExactAmountsVisible
                && Money(entry.Amount, entry.Currency) is { } amount) parts.Add(amount);

            b.AppendLine($"- {Date(entry.OccurredOn)}: {string.Join(" · ", parts)}");
        }
    }

    private static void Achievements(StringBuilder b, AchievementListResponse achievements)
    {
        if (achievements.Items.Count == 0) return;

        b.AppendLine();
        b.AppendLine("BAŞARI, YATIRIM VE FİNANS KAYITLARI");

        var showAmounts = achievements.ExactAmountsVisible;

        foreach (var a in achievements.Items.OrderBy(a => a.OccurredOn))
        {
            var parts = new List<string> { a.KindLabel };

            if (!string.IsNullOrWhiteSpace(a.RoundTypeLabel)) parts.Add($"{a.RoundTypeLabel} turu");
            if (!string.IsNullOrWhiteSpace(a.InstitutionLabel)) parts.Add(a.InstitutionLabel!);
            if (!string.IsNullOrWhiteSpace(a.ProgramName)) parts.Add($"{a.ProgramName} desteği");
            if (!string.IsNullOrWhiteSpace(a.AwardName)) parts.Add(a.AwardName!);
            if (!string.IsNullOrWhiteSpace(a.Organization)) parts.Add($"veren kurum: {a.Organization}");
            if (a.Rank is { } rank) parts.Add($"{rank}. sıra");
            if (!string.IsNullOrWhiteSpace(a.PeriodLabel)) parts.Add($"{a.PeriodLabel} dönemi");

            // "Tutar yok" ile "tutarı göremiyorsun" ayrımı metne de taşınıyor:
            // ikisi aynı cümleyle anlatılırsa rapor yanlış bilgi verir.
            if (a.AmountMasked || !showAmounts) parts.Add("tutar bu raporda gizli tutuluyor");
            else if (Money(a.Amount, a.Currency) is { } amount) parts.Add($"tutar {amount}");

            if (showAmounts && Money(a.Valuation, a.Currency) is { } valuation)
                parts.Add($"şirket değerlemesi {valuation}");
            if (a.InvestorNames.Count > 0) parts.Add($"yatırımcılar: {string.Join(", ", a.InvestorNames)}");
            if (a.TargetCountries.Count > 0) parts.Add($"hedef ülkeler: {string.Join(", ", a.TargetCountries)}");
            if (!string.IsNullOrWhiteSpace(a.Note)) parts.Add($"not: {a.Note}");

            parts.Add(a.IsVerified
                ? "yetkili tarafından doğrulandı"
                : "yetkili onayı bekliyor");

            b.AppendLine($"- {Date(a.OccurredOn)}: {a.Title} ({string.Join(" · ", parts)})");
        }
    }

    private static void Line(StringBuilder b, string label, string? value)
    {
        // Değeri olmayan satır hiç yazılmıyor: "Logo: -" gibi bir satır modele
        // yine "burada bir alan var ve boş" bilgisini verir ve rapora sızar.
        if (!string.IsNullOrWhiteSpace(value))
            b.AppendLine($"{label}: {value.Trim()}");
    }

    private static string Count(int value) => value.ToString(Turkish);

    private static string? Date(DateOnly? value) =>
        value?.ToString("d MMMM yyyy", Turkish);

    private static string? Date(DateTimeOffset? value) =>
        value?.ToString("d MMMM yyyy", Turkish);

    /// <summary>
    /// Tutar metni. Kültür açıkça tr-TR: sunucunun yerel ayarına bırakılan
    /// biçimlendirme makineye bağlı çıktı üretir (bkz. CLAUDE.md).
    /// </summary>
    private static string? Money(decimal? amount, string? currency)
    {
        if (amount is not { } value) return null;

        var text = value.ToString("#,##0.##", Turkish);

        return currency switch
        {
            null or "" => text,
            StartupMoney.ReportingCurrency => $"{text} TL",
            _ => $"{text} {currency}"
        };
    }
}
