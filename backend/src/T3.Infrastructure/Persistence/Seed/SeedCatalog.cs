using T3.Domain.Programs;
using T3.Domain.Startups;

namespace T3.Infrastructure.Persistence.Seed;

internal sealed record TeamBlueprint(
    string FullName, string Title, bool IsFounder, DateOnly JoinedOn);

internal sealed record StartupBlueprint(
    string Name,
    string LegalName,
    string TaxNumber,
    Sector Sector,
    DateOnly FoundedOn,
    string City,
    string[] TechnologyAreas,
    string ProductDescription,
    string Website,
    StartupStatus Status,
    TeamBlueprint[] Team);

internal sealed record ProgramBlueprint(
    string Name, ProgramType Type, string Coordinatorship, string Description,
    (string Name, DateOnly StartsOn, DateOnly? EndsOn)[] Terms);

/// <summary>
/// Demo veri kataloğu. Tüm kayıtlar kurgudur; e-posta ve alan adları
/// <c>.test</c> uzantısını (RFC 6761 ile ayrılmış) kullanır, telefonlar
/// tahsis edilmemiş bir öneki taşır — böylece hiçbir gerçek kişi ya da
/// kurumun verisiyle karıştırılamaz.
/// </summary>
internal static class SeedCatalog
{
    public const string EmailDomain = "t3ekosistem.test";

    public static readonly ProgramBlueprint[] Programs =
    [
        new("T3 Ön Kuluçka", ProgramType.PreIncubation, "Girişim Merkezi",
            "Fikir aşamasındaki bursiyer takımlarını doğrulama sürecine hazırlayan program.",
            [("2024 Güz", new DateOnly(2024, 9, 15), new DateOnly(2025, 1, 31)),
             ("2025 Bahar", new DateOnly(2025, 2, 17), new DateOnly(2025, 6, 30)),
             ("2026 Bahar", new DateOnly(2026, 2, 16), null)]),

        new("T3 Kuluçka", ProgramType.Incubation, "Girişim Merkezi",
            "Ürünü doğrulanmış girişimlere ofis, mentörlük ve yatırıma hazırlık desteği.",
            [("2024 Dönemi", new DateOnly(2024, 3, 1), new DateOnly(2025, 2, 28)),
             ("2025 Dönemi", new DateOnly(2025, 3, 3), null)]),

        new("T3 Hızlandırma", ProgramType.Acceleration, "Girişim Merkezi",
            "Ölçeklenme aşamasındaki girişimler için yatırımcı ağı ve ihracat programı.",
            [("2025 Kohort 1", new DateOnly(2025, 4, 7), new DateOnly(2025, 10, 3))]),

        new("TEKNOFEST", ProgramType.Competition, "TEKNOFEST Koordinatörlüğü",
            "Havacılık, uzay ve teknoloji festivali yarışmaları.",
            [("TEKNOFEST 2024", new DateOnly(2024, 5, 1), new DateOnly(2024, 10, 6)),
             ("TEKNOFEST 2025", new DateOnly(2025, 5, 1), new DateOnly(2025, 9, 21))]),

        new("DENEYAP Türkiye", ProgramType.Training, "DENEYAP Koordinatörlüğü",
            "Teknoloji atölyeleri mezunlarına yönelik ileri seviye eğitim programı.",
            [("2025 Atölye Dönemi", new DateOnly(2025, 1, 13), new DateOnly(2025, 12, 19))])
    ];

    /// <summary>
    /// İlk altı girişim Ön Kuluçka/Kuluçka programlarına, kalanlar yalnızca
    /// TEKNOFEST, DENEYAP ve Hızlandırma'ya bağlanır. Bu ayrım kasıtlı: Program
    /// Yöneticisi rolünün kapsam daraltması demoda gözle görülür olsun.
    /// </summary>
    public static readonly StartupBlueprint[] Startups =
    [
        new("Anadolu Robotik", "Anadolu Robotik Teknolojileri A.Ş.", "1234567801",
            Sector.Manufacturing, new DateOnly(2022, 4, 12), "Ankara",
            ["Otonom sistemler", "Endüstriyel robot kolu", "Bilgisayarlı görü"],
            "Üretim hatlarında insan-robot iş birliğini mümkün kılan kuvvet geri beslemeli robot kol ve kontrol yazılımı.",
            "https://anadolurobotik.test", StartupStatus.Active,
            [new("Elif Yıldırım", "Kurucu ortak, CEO", true, new DateOnly(2022, 4, 12)),
             new("Mert Akgün", "Kurucu ortak, CTO", true, new DateOnly(2022, 4, 12)),
             new("Deniz Korkmaz", "Kıdemli Gömülü Yazılım Mühendisi", false, new DateOnly(2023, 2, 6))]),

        new("Kuzey Sensör Teknolojileri", "Kuzey Sensör Sanayi ve Ticaret A.Ş.", "1234567802",
            Sector.Defense, new DateOnly(2021, 11, 3), "Kocaeli",
            ["MEMS sensör", "Ataletsel ölçüm", "Sinyal işleme"],
            "Kritik platformlar için yerli MEMS tabanlı ataletsel ölçüm birimi (IMU) ve kalibrasyon altyapısı.",
            "https://kuzeysensor.test", StartupStatus.Active,
            [new("Burak Şahin", "Kurucu, Genel Müdür", true, new DateOnly(2021, 11, 3)),
             new("Ayşe Demirtaş", "Ar-Ge Direktörü", false, new DateOnly(2022, 6, 20))]),

        new("Marmara Biyoteknoloji", "Marmara Biyoteknoloji Ar-Ge Ltd. Şti.", "1234567803",
            Sector.Health, new DateOnly(2023, 1, 24), "İstanbul",
            ["Moleküler teşhis", "Biyoinformatik", "Hızlı test kiti"],
            "Sahada 20 dakikada sonuç veren izotermal amplifikasyon tabanlı patojen teşhis platformu.",
            "https://marmarabiyo.test", StartupStatus.Active,
            [new("Zeynep Aksoy", "Kurucu, Bilim Direktörü", true, new DateOnly(2023, 1, 24)),
             new("Hakan Erdoğan", "Kurucu ortak, Operasyon", true, new DateOnly(2023, 1, 24)),
             new("Selin Bozkurt", "Biyoinformatik Uzmanı", false, new DateOnly(2024, 3, 11))]),

        new("Ege Tarım Verisi", "Ege Tarım Verisi Yazılım A.Ş.", "1234567804",
            Sector.Agriculture, new DateOnly(2022, 8, 30), "İzmir",
            ["Uydu görüntü analizi", "Makine öğrenmesi", "IoT sensör ağı"],
            "Uydu ve saha sensörü verisini birleştirerek parsel bazında sulama ve gübreleme önerisi üreten karar destek sistemi.",
            "https://egetarimverisi.test", StartupStatus.Active,
            [new("Onur Kaplan", "Kurucu, CEO", true, new DateOnly(2022, 8, 30)),
             new("Ceren Tunç", "Veri Bilimi Lideri", false, new DateOnly(2023, 5, 2))]),

        new("Selçuk Enerji Sistemleri", "Selçuk Enerji Sistemleri Ltd. Şti.", "1234567805",
            Sector.Energy, new DateOnly(2021, 6, 18), "Konya",
            ["Batarya yönetim sistemi", "Güç elektroniği", "Şebeke entegrasyonu"],
            "Ticari ölçekli güneş santralleri için batarya yönetim yazılımı ve iki yönlü dönüştürücü donanımı.",
            "https://selcukenerji.test", StartupStatus.Graduated,
            [new("İsmail Yavuz", "Kurucu, Genel Müdür", true, new DateOnly(2021, 6, 18)),
             new("Nazlı Çetin", "Güç Elektroniği Mühendisi", false, new DateOnly(2022, 1, 10))]),

        new("Karadeniz Görüntü İşleme", "Karadeniz Görüntü İşleme Yazılım Ltd. Şti.", "1234567806",
            Sector.Software, new DateOnly(2023, 9, 5), "Trabzon",
            ["Kenar yapay zekâ", "Nesne takibi", "Video analitiği"],
            "Düşük güçlü kenar cihazlarda çalışan, internet bağlantısı gerektirmeyen video analitiği kütüphanesi.",
            "https://karadenizgoruntu.test", StartupStatus.Active,
            [new("Emre Balcı", "Kurucu, CTO", true, new DateOnly(2023, 9, 5)),
             new("Gizem Arslan", "Kurucu ortak, Ürün", true, new DateOnly(2023, 9, 5))]),

        new("Toros Uzay Bileşenleri", "Toros Uzay Bileşenleri A.Ş.", "1234567807",
            Sector.Space, new DateOnly(2020, 3, 9), "Adana",
            ["Uydu alt sistemleri", "Termal tasarım", "Test altyapısı"],
            "Küp uydular için ısıl kontrol modülü ve yer istasyonu haberleşme kartı üretimi.",
            "https://torosuzay.test", StartupStatus.Active,
            [new("Kemal Doğan", "Kurucu, Genel Müdür", true, new DateOnly(2020, 3, 9)),
             new("Pınar Uzun", "Sistem Mühendisi", false, new DateOnly(2021, 9, 15))]),

        new("Boğaz Finans Teknolojileri", "Boğaz Finans Teknolojileri A.Ş.", "1234567808",
            Sector.Finance, new DateOnly(2022, 2, 14), "İstanbul",
            ["Açık bankacılık", "Risk skorlama", "Dolandırıcılık tespiti"],
            "KOBİ'lere gerçek zamanlı nakit akışı tahmini ve kredi risk skoru sunan açık bankacılık altyapısı.",
            "https://bogazfintek.test", StartupStatus.Active,
            [new("Sinem Polat", "Kurucu, CEO", true, new DateOnly(2022, 2, 14)),
             new("Yusuf Kara", "Kurucu ortak, CTO", true, new DateOnly(2022, 2, 14)),
             new("Ali Rıza Şen", "Risk Analisti", false, new DateOnly(2023, 8, 21))]),

        new("Kapadokya Eğitim Teknolojileri", "Kapadokya Eğitim Teknolojileri Ltd. Şti.", "1234567809",
            Sector.Education, new DateOnly(2023, 4, 3), "Nevşehir",
            ["Uyarlanabilir öğrenme", "Doğal dil işleme", "Ölçme değerlendirme"],
            "Öğrencinin hata örüntüsünü analiz ederek kişiye özel çalışma planı üreten uyarlanabilir öğrenme platformu.",
            "https://kapadokyaedtech.test", StartupStatus.Active,
            [new("Merve Şimşek", "Kurucu, CEO", true, new DateOnly(2023, 4, 3)),
             new("Tolga Ateş", "Öğrenme Bilimleri Uzmanı", false, new DateOnly(2024, 1, 8))]),

        new("Trakya Lojistik Zekâsı", "Trakya Lojistik Zekâsı Yazılım A.Ş.", "1234567810",
            Sector.Mobility, new DateOnly(2021, 10, 27), "Tekirdağ",
            ["Rota optimizasyonu", "Filo telemetrisi", "Talep tahmini"],
            "Soğuk zincir taşımacılığı için rota optimizasyonu ve gerçek zamanlı sıcaklık ihlali uyarı sistemi.",
            "https://trakyalojistik.test", StartupStatus.Active,
            [new("Cem Öztürk", "Kurucu, Genel Müdür", true, new DateOnly(2021, 10, 27)),
             new("Ebru Kılıç", "Optimizasyon Araştırmacısı", false, new DateOnly(2022, 11, 14))]),

        new("Fırat Malzeme Bilimi", "Fırat Malzeme Bilimi Ar-Ge A.Ş.", "1234567811",
            Sector.Manufacturing, new DateOnly(2020, 12, 1), "Elazığ",
            ["Kompozit malzeme", "Eklemeli üretim", "Yüzey kaplama"],
            "Havacılık uygulamaları için yüksek sıcaklığa dayanıklı seramik matrisli kompozit üretim yöntemi.",
            "https://firatmalzeme.test", StartupStatus.Active,
            [new("Hüseyin Aydın", "Kurucu, Bilim Direktörü", true, new DateOnly(2020, 12, 1)),
             new("Şeyma Güler", "Malzeme Mühendisi", false, new DateOnly(2022, 4, 25))]),

        new("Van Su Teknolojileri", "Van Su Teknolojileri Ltd. Şti.", "1234567812",
            Sector.Other, new DateOnly(2024, 2, 20), "Van",
            ["Membran filtrasyon", "Su kalitesi izleme", "Uzaktan telemetri"],
            "Kırsal yerleşimler için şebekeden bağımsız, güneş enerjili membran filtrasyon ünitesi.",
            "https://vansuteknoloji.test", StartupStatus.Active,
            [new("Rojda Yılmaz", "Kurucu, CEO", true, new DateOnly(2024, 2, 20)),
             new("Barış Sezer", "Proses Mühendisi", false, new DateOnly(2024, 9, 2))])
    ];
}
