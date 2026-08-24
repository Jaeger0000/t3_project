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
    /// Girişim kataloğu. Hangi girişimin hangi programa bağlandığı burada değil
    /// <see cref="DevDataSeeder"/> içindeki katılım planında durur; oradaki
    /// dağılım Program Yöneticisi rolünün kapsam daraltmasını demoda gözle
    /// görülür kılacak şekilde kurgulanmıştır.
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
             new("Barış Sezer", "Proses Mühendisi", false, new DateOnly(2024, 9, 2))]),

        // --- Faz 5: demo ölçeği ------------------------------------------
        // Pano ve grafikler ancak yeterli çeşitlilikte veriyle anlamlı okunur;
        // aşağıdaki girişimler sektör, şehir, durum ve olgunluk dağılımını
        // genişletmek için eklendi.

        new("Sakarya Batarya", "Sakarya Batarya Teknolojileri A.Ş.", "1234567813",
            Sector.Energy, new DateOnly(2021, 3, 8), "Sakarya",
            ["Lityum hücre", "Termal yönetim", "Hızlı şarj"],
            "Elektrikli ticari araçlar için yüksek çevrim ömürlü hücre paketi ve termal yönetim modülü.",
            "https://sakaryabatarya.test", StartupStatus.Active,
            [new("Uğur Keskin", "Kurucu, CEO", true, new DateOnly(2021, 3, 8)),
             new("Melis Turan", "Hücre Kimyası Uzmanı", false, new DateOnly(2022, 5, 16))]),

        new("Gaziantep Gıda Teknolojileri", "Gaziantep Gıda Teknolojileri Ltd. Şti.", "1234567814",
            Sector.Agriculture, new DateOnly(2022, 6, 21), "Gaziantep",
            ["Gıda güvenliği", "Hızlı analiz", "Soğuk zincir"],
            "Gıda üretim hatlarında mikotoksin ve kalıntı analizini sahada yapan taşınabilir cihaz.",
            "https://gaziantepgida.test", StartupStatus.Active,
            [new("Fatma Nur Çelik", "Kurucu, Genel Müdür", true, new DateOnly(2022, 6, 21)),
             new("Halil İbrahim Tan", "Analitik Kimya Uzmanı", false, new DateOnly(2023, 4, 3))]),

        new("Bursa Otonom Sürüş", "Bursa Otonom Sürüş Sistemleri A.Ş.", "1234567815",
            Sector.Mobility, new DateOnly(2020, 9, 14), "Bursa",
            ["Sensör füzyonu", "Sürüş algoritmaları", "Simülasyon"],
            "Kapalı saha araçları için seviye 4 otonom sürüş yazılımı ve doğrulama simülatörü.",
            "https://bursaotonom.test", StartupStatus.Graduated,
            [new("Serkan Aydemir", "Kurucu, CTO", true, new DateOnly(2020, 9, 14)),
             new("Nihan Özkan", "Kurucu ortak, CEO", true, new DateOnly(2020, 9, 14)),
             new("Kaan Yücel", "Kıdemli Algoritma Mühendisi", false, new DateOnly(2022, 2, 1))]),

        new("Eskişehir Havacılık Yazılımı", "Eskişehir Havacılık Yazılımı Ltd. Şti.", "1234567816",
            Sector.Space, new DateOnly(2022, 11, 9), "Eskişehir",
            ["Uçuş kontrol yazılımı", "DO-178C", "Gömülü sistem"],
            "Sivil havacılık sertifikasyonuna uygun uçuş kontrol yazılımı geliştirme ve doğrulama araçları.",
            "https://eskisehirhavacilik.test", StartupStatus.Active,
            [new("Tuğçe Bilgin", "Kurucu, Genel Müdür", true, new DateOnly(2022, 11, 9)),
             new("Ozan Demir", "Sertifikasyon Uzmanı", false, new DateOnly(2023, 7, 24))]),

        new("Antalya Turizm Verisi", "Antalya Turizm Verisi Yazılım Ltd. Şti.", "1234567817",
            Sector.Software, new DateOnly(2023, 5, 30), "Antalya",
            ["Talep tahmini", "Dinamik fiyatlama", "Veri görselleştirme"],
            "Konaklama işletmeleri için doluluk tahmini ve dinamik fiyatlama karar destek paneli.",
            "https://antalyaturizmverisi.test", StartupStatus.Active,
            [new("Ecem Karataş", "Kurucu, CEO", true, new DateOnly(2023, 5, 30))]),

        new("Kayseri Savunma Elektroniği", "Kayseri Savunma Elektroniği A.Ş.", "1234567818",
            Sector.Defense, new DateOnly(2019, 7, 2), "Kayseri",
            ["RF tasarım", "Elektronik harp", "Anten dizisi"],
            "Taktik platformlar için yazılım tanımlı telsiz ve geniş bantlı anten dizisi.",
            "https://kayserisavunma.test", StartupStatus.Active,
            [new("Ahmet Kurt", "Kurucu, Genel Müdür", true, new DateOnly(2019, 7, 2)),
             new("Büşra Yalçın", "RF Tasarım Mühendisi", false, new DateOnly(2020, 10, 12)),
             new("Emirhan Sarı", "Test Mühendisi", false, new DateOnly(2023, 1, 16))]),

        new("Denizli Tekstil Zekâsı", "Denizli Tekstil Zekâsı Yazılım Ltd. Şti.", "1234567819",
            Sector.Manufacturing, new DateOnly(2023, 2, 13), "Denizli",
            ["Kusur tespiti", "Bilgisayarlı görü", "Üretim izleme"],
            "Dokuma hatlarında gerçek zamanlı kusur tespiti yapan kamera ve analiz sistemi.",
            "https://denizlitekstil.test", StartupStatus.Active,
            [new("Gökhan Aslan", "Kurucu, CTO", true, new DateOnly(2023, 2, 13)),
             new("Sevgi Duran", "Görüntü İşleme Mühendisi", false, new DateOnly(2024, 5, 6))]),

        new("Samsun Deniz Teknolojileri", "Samsun Deniz Teknolojileri A.Ş.", "1234567820",
            Sector.Other, new DateOnly(2021, 8, 25), "Samsun",
            ["İnsansız deniz aracı", "Sonar", "Deniz kirliliği izleme"],
            "Kıyı sularında kirlilik ve batimetri ölçümü yapan insansız deniz aracı.",
            "https://samsundeniz.test", StartupStatus.Active,
            [new("Yiğit Erdem", "Kurucu, CEO", true, new DateOnly(2021, 8, 25)),
             new("Aslı Kaya", "Deniz Sistemleri Mühendisi", false, new DateOnly(2022, 12, 5))]),

        new("Malatya Tarım Robotları", "Malatya Tarım Robotları Ltd. Şti.", "1234567821",
            Sector.Agriculture, new DateOnly(2023, 10, 17), "Malatya",
            ["Hasat robotu", "Manipülatör", "Meyve tanıma"],
            "Kayısı ve elma bahçeleri için hasat robotu ve olgunluk tanıma yazılımı.",
            "https://malatyatarimrobot.test", StartupStatus.Active,
            [new("Zehra Aktaş", "Kurucu, CEO", true, new DateOnly(2023, 10, 17)),
             new("Furkan Coşkun", "Robotik Mühendisi", false, new DateOnly(2024, 6, 10))]),

        new("Diyarbakır Güneş Enerjisi", "Diyarbakır Güneş Enerjisi A.Ş.", "1234567822",
            Sector.Energy, new DateOnly(2022, 4, 5), "Diyarbakır",
            ["İzleyici sistem", "Panel temizleme", "Saha otomasyonu"],
            "Büyük ölçekli güneş santralleri için otonom panel temizleme robotu ve izleyici kontrolü.",
            "https://diyarbakirgunes.test", StartupStatus.Active,
            [new("Mehmet Şirin", "Kurucu, Genel Müdür", true, new DateOnly(2022, 4, 5)),
             new("Havva Ekinci", "Otomasyon Mühendisi", false, new DateOnly(2023, 9, 18))]),

        new("İzmir Sağlık Yapay Zekâsı", "İzmir Sağlık Yapay Zekâsı Ltd. Şti.", "1234567823",
            Sector.Health, new DateOnly(2022, 12, 12), "İzmir",
            ["Radyoloji", "Derin öğrenme", "Klinik karar destek"],
            "Akciğer tomografilerinde nodül tespitini önceliklendiren klinik karar destek yazılımı.",
            "https://izmirsaglikai.test", StartupStatus.Active,
            [new("Doğukan Şen", "Kurucu, CTO", true, new DateOnly(2022, 12, 12)),
             new("Elif Naz Yıldız", "Klinik Araştırma Uzmanı", false, new DateOnly(2023, 11, 20))]),

        new("Konya Hidrojen", "Konya Hidrojen Sistemleri A.Ş.", "1234567824",
            Sector.Energy, new DateOnly(2023, 7, 7), "Konya",
            ["Elektrolizör", "Yakıt hücresi", "Depolama"],
            "Tarımsal sulama pompaları için yeşil hidrojen üretim ve depolama ünitesi.",
            "https://konyahidrojen.test", StartupStatus.Active,
            [new("Ramazan Öz", "Kurucu, CEO", true, new DateOnly(2023, 7, 7))]),

        new("Ankara Siber Kalkan", "Ankara Siber Kalkan Bilişim A.Ş.", "1234567825",
            Sector.Software, new DateOnly(2021, 1, 19), "Ankara",
            ["Saldırı tespiti", "Sıfır güven", "Endüstriyel siber güvenlik"],
            "Endüstriyel kontrol sistemleri için pasif ağ izleme ve anomali tespiti platformu.",
            "https://ankarasiberkalkan.test", StartupStatus.Active,
            [new("Cansu Bulut", "Kurucu, CEO", true, new DateOnly(2021, 1, 19)),
             new("Tarık Genç", "Kurucu ortak, Güvenlik Araştırmacısı", true, new DateOnly(2021, 1, 19))]),

        new("Mersin Liman Otomasyonu", "Mersin Liman Otomasyonu Ltd. Şti.", "1234567826",
            Sector.Mobility, new DateOnly(2022, 9, 28), "Mersin",
            ["Konteyner takibi", "Vinç otomasyonu", "Saha optimizasyonu"],
            "Konteyner terminallerinde saha aracı yönlendirme ve vinç sırası optimizasyonu.",
            "https://mersinliman.test", StartupStatus.Active,
            [new("Hasan Ali Yaman", "Kurucu, Genel Müdür", true, new DateOnly(2022, 9, 28)),
             new("Damla Erol", "Operasyon Araştırmacısı", false, new DateOnly(2023, 6, 12))]),

        new("Erzurum Soğuk İklim Teknolojileri", "Erzurum Soğuk İklim Teknolojileri Ltd. Şti.", "1234567827",
            Sector.Other, new DateOnly(2024, 1, 15), "Erzurum",
            ["Isı pompası", "Yalıtım", "Enerji verimliliği"],
            "Sert kış koşullarında verimini koruyan düşük sıcaklık ısı pompası tasarımı.",
            "https://erzurumsogukiklim.test", StartupStatus.Active,
            [new("Nurcan Aslan", "Kurucu, CEO", true, new DateOnly(2024, 1, 15))]),

        new("Adana Su Yönetimi", "Adana Su Yönetimi Yazılım Ltd. Şti.", "1234567828",
            Sector.Software, new DateOnly(2021, 5, 11), "Adana",
            ["Kaçak tespiti", "Şebeke modelleme", "Akustik sensör"],
            "İçme suyu şebekelerinde akustik sensörlerle kaçak tespiti ve önceliklendirme.",
            "https://adanasuyonetimi.test", StartupStatus.Exited,
            [new("Volkan Şahin", "Kurucu, CEO", true, new DateOnly(2021, 5, 11)),
             new("Merve Acar", "Hidrolik Mühendisi", false, new DateOnly(2022, 3, 21))]),

        new("Kocaeli Kompozit", "Kocaeli Kompozit Sanayi A.Ş.", "1234567829",
            Sector.Manufacturing, new DateOnly(2020, 6, 30), "Kocaeli",
            ["Karbon fiber", "Otoklav", "Hafifletme"],
            "Raylı sistem ve savunma araçları için karbon fiber gövde bileşenleri üretimi.",
            "https://kocaelikompozit.test", StartupStatus.Active,
            [new("İbrahim Polat", "Kurucu, Genel Müdür", true, new DateOnly(2020, 6, 30)),
             new("Şevval Kara", "Kompozit Süreç Mühendisi", false, new DateOnly(2021, 11, 8))]),

        new("Rize Çay Teknolojisi", "Rize Çay Teknolojisi Ltd. Şti.", "1234567830",
            Sector.Agriculture, new DateOnly(2023, 8, 22), "Rize",
            ["Hasat makinesi", "Kalite tasnifi", "Kurutma"],
            "Çay yaprağında filiz kalitesini görüntüyle tasnifleyen hasat sonrası hat sistemi.",
            "https://rizecayteknoloji.test", StartupStatus.Active,
            [new("Ayşenur Kalyoncu", "Kurucu, CEO", true, new DateOnly(2023, 8, 22))]),

        new("Şanlıurfa Kuraklık Verisi", "Şanlıurfa Kuraklık Verisi Ltd. Şti.", "1234567831",
            Sector.Agriculture, new DateOnly(2024, 4, 9), "Şanlıurfa",
            ["Uzaktan algılama", "Kuraklık indeksi", "Erken uyarı"],
            "Uydu verisinden parsel bazında kuraklık riski üreten erken uyarı servisi.",
            "https://sanliurfakuraklik.test", StartupStatus.Active,
            [new("Ali Kemal Doğru", "Kurucu, CEO", true, new DateOnly(2024, 4, 9))]),

        new("İstanbul Kuantum Yazılımı", "İstanbul Kuantum Yazılımı A.Ş.", "1234567832",
            Sector.Software, new DateOnly(2024, 6, 3), "İstanbul",
            ["Kuantum algoritma", "Optimizasyon", "Simülatör"],
            "Lojistik ve portföy optimizasyonu için kuantumdan esinlenen algoritma kütüphanesi.",
            "https://istanbulkuantum.test", StartupStatus.Active,
            [new("Berk Uçar", "Kurucu, CTO", true, new DateOnly(2024, 6, 3)),
             new("Sude Nur Aydın", "Araştırmacı", false, new DateOnly(2025, 1, 13))])
    ];
}
