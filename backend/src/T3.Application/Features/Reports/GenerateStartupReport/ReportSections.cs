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
    /// <summary>
    /// Her bölüme eklenen biçim ve dil talimatı.
    ///
    /// İkinci paragraf 1 Ekim 2026'daki bir canlı rapordan sonra yazıldı:
    /// metinde "En görünür boşluk, logoUrl alanının null olması" cümlesi
    /// vardı. Eski talimat "bir alan boş/maskeli geldiyse bunu açıkça söyle"
    /// diyordu — yani modeli tam olarak buna davet ediyordu. Artık eksik
    /// bilginin nasıl anlatılacağı kanıt paketinin sonundaki iş dilindeki
    /// listeye bağlanıyor (bkz. <see cref="ReportEvidence"/>).
    /// </summary>
    public const string FormatInstruction =
        "Türkçe yaz ve basılı bir yönetim raporunun diline uy: akıcı paragraflar, "
        + "gerekirse '- ' ile başlayan kısa madde imleri. Başlık işareti (#), tablo (|) "
        + "ya da kod işareti kullanma; vurgu için **kalın metin** kullanabilirsin.\n\n"
        + "Teknik dil kullanma. Alan adı (logoUrl, isVerified, taxNumber gibi), 'null', "
        + "'true/false', JSON, veri yapısı, veri tabanı, ekran ya da sistem terimleri bu "
        + "rapora giremez. Aşağıdaki bilgi senin için hazırlanmış bir brif: brifin "
        + "kendisinden, başlıklarından ya da biçiminden söz etme, yalnızca içeriğini "
        + "kullanarak yaz.\n\n"
        + "Bir bilgi brifte yoksa ondan hiç söz etmeyebilirsin. Söz edeceksen iş diliyle "
        + "anlat: brifin sonundaki 'kayıtlarda bulunmayan bilgiler' listesindeki "
        + "ifadeleri kullan ('girişimin logosu sisteme henüz yüklenmemiş' gibi). "
        + "Sana verilmeyen hiçbir sayıyı, tarihi ya da ismi uydurma.";

    /// <summary>
    /// Metinde teknik ifade yakalandığında bölümü ikinci kez isterken
    /// eklenen düzeltme. Modelin ilk denemesini de konuşmaya koyuyoruz:
    /// "şunu yazdın, böyle olmaz" demek, talimatı tekrar etmekten belirgin
    /// biçimde daha iyi sonuç veriyor.
    /// </summary>
    public const string RewriteInstruction =
        "Yazdığın metinde teknik ifadeler var (alan adı, 'null' gibi sistem terimleri ya "
        + "da kod işareti). Bu metin doğrudan basılı bir rapora giriyor; bu ifadeler "
        + "orada bulunamaz. Aynı bölümü baştan yaz: aynı bilgileri koru ama tamamen "
        + "iş diliyle anlat. Eksik bir bilgiden söz ediyorsan 'şu bilgi sisteme henüz "
        + "girilmemiş' biçiminde yaz. Yalnızca raporun yeni metnini döndür.";

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
            "Bu girişimin büyümesi için 3-5 somut öneri yaz. Her öneri bir eylem "
            + "cümlesiyle başlasın ve neden işe yarayacağını kısaca söylesin "
            + "(ör. 'Girişimin logosunun ve web sitesinin profile eklenmesi, "
            + "yatırımcı görüşmelerinde ekosistem vitrininde görünürlüğü artırır'). "
            + "Önerileri brifteki somut durumlara dayandır — tamamlanmamış profil "
            + "bilgisi, yüklenmemiş doküman, katılınabilecek bir program, doğrulanmayı "
            + "bekleyen kayıt gibi. Eksik bilgileri sıralayan bir döküm yazma; her "
            + "eksikliği girişimin ne kazanacağına bağlayan bir tavsiyeye çevir. "
            + "Genel geçer tavsiye verme."),

        new("EkipVeFaaliyetler", "Ekip ve Faaliyetler",
            "Ekibin bileşimini anlat: kaç kişi, kaçı kurucu, hangi unvanlar var, "
            + "ekibe katılım tarihleri ekibin zaman içinde nasıl büyüdüğünü "
            + "gösteriyor. Kişi adları gizlilik gereği bu rapora dâhil edilmiyor; "
            + "ekibi unvan ve tarihlerle anlat. Bölümün sonunda tek cümleyle, "
            + "adların kişisel verilerin korunması gereği paylaşılmadığını "
            + "belirtebilirsin."),

        new("ProgramGecmisi", "Program Geçmişi ve Kronoloji",
            "Girişimin kuruluşundan bugüne kadarki gelişimini KRONOLOJİK sırayla "
            + "anlat: program katılımları, kilometre taşları, yatırım/hibe/ödül gibi "
            + "olayları tarihleriyle birlikte ver."),

        new("BasariVeYatirim", "Başarı, Yatırım ve Finans Durumu",
            "Girişimin cirosu, ihracatı, yatırım turları, hibeleri ve ödülleri "
            + "hakkında ayrıntılı bilgi ver. Brifte bir tutarın gizli tutulduğu "
            + "yazıyorsa sayı yerine 'tutar bu raporda paylaşılmıyor' diyebilirsin; "
            + "asla sayı uydurma."),
    ];

    public static ReportSectionDefinition? Find(string key) =>
        Standard.FirstOrDefault(s => string.Equals(s.Key, key, StringComparison.OrdinalIgnoreCase));
}
