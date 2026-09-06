namespace T3.Application.Features.Reports.GenerateStartupReport;

/// <summary>
/// Bir rapor bölümü tanımı: anahtar (istemcinin seçtiği), başlık (PDF'te
/// basılan) ve modele verilen talimat.
/// </summary>
public sealed record ReportSectionDefinition(string Key, string Title, string Instruction);

/// <summary>
/// Standart rapor bölümleri. Her biri modele AYRI bir çağrıda gidiyor —
/// kullanıcının isteği bu ("her başlığa tüm veriyi kullanarak ayrı cevap
/// versin"). Sıra burada tanımlı sıradır, PDF'te de aynı sırayla basılır.
///
/// Talimatlar bilinçli olarak "düz paragraf + kısa madde imi + **kalın**"
/// dışında biçim istemiyor (tablo, başlık işareti yok): PDF tarafındaki
/// basit ayrıştırıcı (bkz. QuestPdfReportRenderer) yalnızca bu alt kümeyi
/// biliyor, model daha zengin bir biçim denerse ekranda bozuk görünürdü.
/// </summary>
public static class ReportSections
{
    public const string FormatInstruction =
        "Türkçe yaz. Düz paragraflar ve gerekirse kısa madde imleri ('- ' ile "
        + "başlayan satırlar) kullan; başlık işareti (#), tablo (|) ya da başka "
        + "bir biçimlendirme kullanma. Vurgu için **kalın metin** kullanabilirsin. "
        + "Yalnızca sana verilen veriyi kullan, tahmin ya da uydurma ekleme; "
        + "bir alan boş/maskeli geldiyse bunu açıkça söyle, sayı uydurma.";

    public static readonly IReadOnlyList<ReportSectionDefinition> Standard =
    [
        new("Ozet", "Yönetici Özeti",
            "Bu girişimin kim olduğunu, ne yaptığını ve genel durumunu 3-5 cümlelik "
            + "kısa bir yönetici özetiyle anlat."),

        new("GucluYonler", "Güçlü Yönler ve Öne Çıkanlar",
            "Verilere dayanarak girişimin öne çıkan güçlü yönlerini, başarılarını ve "
            + "olumlu gelişmelerini yaz. Girişimdeki yöneticinin görünce memnun "
            + "olacağı somut noktaları (doğrulanmış başarılar, tamamlanan programlar, "
            + "büyüyen ekip vb.) vurgula."),

        new("Oneriler", "Gelişim Önerileri",
            "Verilere dayanarak bu girişimin büyümesi ve gelişmesi için somut, "
            + "uygulanabilir öneriler yaz — örneğin eksik doküman, katılabileceği "
            + "yeni bir program, tamamlanmamış profil alanı, iyileştirilebilecek bir "
            + "gösterge gibi. Genel geçer tavsiye değil, verideki somut boşluklara "
            + "dayalı öneri yaz."),

        new("EkipVeFaaliyetler", "Ekip ve Faaliyetler",
            "Ekip üyelerini, unvanlarını ve katılım tarihlerini özetle; kimin "
            + "girişimde hangi konumda ve ne zamandan beri olduğunu anlat."),

        new("ProgramGecmisi", "Program Geçmişi ve Kronoloji",
            "Girişimin kuruluşundan bugüne kadarki gelişimini KRONOLOJİK sırayla "
            + "anlat: program katılımları, kilometre taşları, yatırım/hibe/ödül gibi "
            + "olayları tarihleriyle birlikte ver."),

        new("BasariVeYatirim", "Başarı, Yatırım ve Finans Durumu",
            "Girişimin ciro, ihracat, yatırım turları, hibe ve ödülleri hakkında "
            + "detaylı bilgi ver. Tutar alanı bu görüntüleyici için maskeliyse "
            + "bunu açıkça belirt, asla sayı uydurma."),
    ];

    public static ReportSectionDefinition? Find(string key) =>
        Standard.FirstOrDefault(s => string.Equals(s.Key, key, StringComparison.OrdinalIgnoreCase));
}
