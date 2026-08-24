using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using T3.Application.Common.Interfaces;
using T3.Application.Features.Achievements;
using T3.Application.Features.Approvals;
using T3.Application.Features.Documents;
using T3.Application.Features.Startups;
using T3.Application.Features.Startups.Team;
using T3.Domain.Achievements;
using T3.Domain.Approvals;
using T3.Domain.Documents;
using T3.Domain.Identity;
using T3.Domain.Milestones;
using T3.Domain.Programs;
using T3.Domain.Startups;

namespace T3.Infrastructure.Persistence.Seed;

/// <summary>
/// Demo ve geliştirme verisini yükler. Boş bir sistem demo edilemez; bu veri
/// kod olarak durur, elle girilmez, böylece veritabanı her sıfırlamada aynı
/// hikâyeyi üretir.
///
/// Fikir mülkiyeti notu: kayıtların tamamı kurgudur, T3 Vakfı'nın gerçek
/// operasyonel verisi bu depoya girmez.
/// </summary>
public sealed class DevDataSeeder(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    IDocumentStorage storage,
    ILogger<DevDataSeeder> logger)
{
    public async Task SeedAsync(string password, CancellationToken ct = default)
    {
        if (await db.Users.IgnoreQueryFilters().AnyAsync(ct))
        {
            logger.LogInformation("Tohum veri atlandı: veritabanında kullanıcı zaten var.");
            return;
        }

        var programs = SeedPrograms();
        await db.SaveChangesAsync(ct);

        var startups = SeedStartups();
        await db.SaveChangesAsync(ct);

        SeedParticipations(startups, programs);
        SeedAchievements(startups);
        SeedMilestones(startups);
        var users = SeedUsers(startups, programs, password);
        SeedChangeRequests(startups, users);
        await SeedDocumentsAsync(startups, users, ct);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Tohum veri yüklendi: {Programs} program, {Startups} girişim, {Users} kullanıcı, "
            + "{Documents} doküman, {ChangeRequests} onay isteği.",
            programs.Count, startups.Length, await db.Users.CountAsync(ct),
            await db.Documents.CountAsync(ct), await db.ChangeRequests.CountAsync(ct));
    }

    // --- Programlar ------------------------------------------------------

    private Dictionary<string, EcosystemProgram> SeedPrograms()
    {
        var programs = new Dictionary<string, EcosystemProgram>(StringComparer.Ordinal);

        foreach (var blueprint in SeedCatalog.Programs)
        {
            var program = new EcosystemProgram
            {
                Name = blueprint.Name,
                Type = blueprint.Type,
                Coordinatorship = blueprint.Coordinatorship,
                Description = blueprint.Description,
                Terms = blueprint.Terms
                    .Select(t => new ProgramTerm
                    {
                        Name = t.Name,
                        StartsOn = t.StartsOn,
                        EndsOn = t.EndsOn
                    })
                    .ToList()
            };

            db.Programs.Add(program);
            programs[blueprint.Name] = program;
        }

        return programs;
    }

    // --- Girişimler ------------------------------------------------------

    private Startup[] SeedStartups()
    {
        var startups = SeedCatalog.Startups
            .Select((blueprint, index) =>
            {
                var host = new Uri(blueprint.Website).Host;

                return new Startup
                {
                    Name = blueprint.Name,
                    LegalName = blueprint.LegalName,
                    TaxNumber = blueprint.TaxNumber,
                    Sector = blueprint.Sector,
                    FoundedOn = blueprint.FoundedOn,
                    City = blueprint.City,
                    TechnologyAreas = [.. blueprint.TechnologyAreas],
                    ProductDescription = blueprint.ProductDescription,
                    Website = blueprint.Website,
                    ContactEmail = $"iletisim@{host}",
                    ContactPhone = $"+90 500 000 00 {index + 1:D2}",
                    Status = blueprint.Status,
                    // Kayıt tarihi kuruluştan sonraya, listedeki sıraya göre
                    // dağıtılır; "en yeni" sıralaması demoda anlamlı olsun.
                    CreatedAt = new DateTimeOffset(
                        blueprint.FoundedOn.ToDateTime(TimeOnly.MinValue),
                        TimeSpan.Zero).AddDays(30 + index),
                    TeamMembers = blueprint.Team
                        .Select((m, memberIndex) => new TeamMember
                        {
                            FullName = m.FullName,
                            Title = m.Title,
                            Email = $"{Slug(m.FullName)}@{host}",
                            Phone = $"+90 500 001 {index + 1:D2} {memberIndex + 1:D2}",
                            LinkedInUrl = $"https://www.linkedin.test/in/{Slug(m.FullName)}",
                            IsFounder = m.IsFounder,
                            JoinedOn = m.JoinedOn
                        })
                        .ToList()
                };
            })
            .ToArray();

        db.Startups.AddRange(startups);
        return startups;
    }

    // --- Program katılımları ---------------------------------------------

    /// <summary>
    /// Katılımlar üç kümeye ayrılır ve bölünme kasıtlıdır:
    /// Ön Kuluçka/Kuluçka kümesi ile TEKNOFEST/DENEYAP/Hızlandırma kümesi iki
    /// farklı Program Yöneticisi hesabına farklı listeler gösterir; hiçbir
    /// programa bağlanmayan girişimler ise yalnızca ekosistem geneline yetkili
    /// rollerde görünür. Üçüncü küme olmadan "kapsam dışı" durumu demoda
    /// gözlemlenemiyor.
    /// </summary>
    private void SeedParticipations(
        Startup[] startups, Dictionary<string, EcosystemProgram> programs)
    {
        (int Startup, string Program, string Term, ParticipationStatus Status, string? Note)[] plan =
        [
            (0, "T3 Ön Kuluçka", "2024 Güz", ParticipationStatus.Graduated,
                "Doğrulama sürecini ilk çeyrekte tamamladı."),
            (0, "T3 Kuluçka", "2025 Dönemi", ParticipationStatus.InProgress,
                "Seri A hazırlığı sürüyor."),
            (1, "T3 Kuluçka", "2024 Dönemi", ParticipationStatus.Graduated, null),
            (2, "T3 Ön Kuluçka", "2025 Bahar", ParticipationStatus.Completed, null),
            (2, "T3 Kuluçka", "2025 Dönemi", ParticipationStatus.InProgress,
                "Klinik doğrulama aşamasında."),
            (3, "T3 Ön Kuluçka", "2024 Güz", ParticipationStatus.Graduated, null),
            (3, "T3 Kuluçka", "2025 Dönemi", ParticipationStatus.InProgress, null),
            (4, "T3 Kuluçka", "2024 Dönemi", ParticipationStatus.Graduated,
                "Programdan mezun oldu, ihracata yöneldi."),
            (5, "T3 Ön Kuluçka", "2026 Bahar", ParticipationStatus.InProgress, null),

            (6, "TEKNOFEST", "TEKNOFEST 2024", ParticipationStatus.Completed, null),
            (6, "T3 Hızlandırma", "2025 Kohort 1", ParticipationStatus.Graduated, null),
            (7, "T3 Hızlandırma", "2025 Kohort 1", ParticipationStatus.Graduated, null),
            (8, "DENEYAP Türkiye", "2025 Atölye Dönemi", ParticipationStatus.InProgress, null),
            (9, "TEKNOFEST", "TEKNOFEST 2025", ParticipationStatus.Completed, null),
            (10, "TEKNOFEST", "TEKNOFEST 2024", ParticipationStatus.Completed, null),
            (10, "TEKNOFEST", "TEKNOFEST 2025", ParticipationStatus.Completed, null),
            (11, "DENEYAP Türkiye", "2025 Atölye Dönemi", ParticipationStatus.InProgress, null),

            // --- Faz 5 demo ölçeği --------------------------------------
            // Dört girişim (26, 29, 30, 31) bilinçli olarak hiçbir programa
            // bağlanmadı: ekosisteme kayıtlı ama henüz programa girmemiş
            // girişimler Program Yöneticisi'nin kapsamında görünmemeli.
            (12, "T3 Kuluçka", "2024 Dönemi", ParticipationStatus.Graduated, null),
            (12, "T3 Hızlandırma", "2025 Kohort 1", ParticipationStatus.Graduated,
                "İhracat programını tamamladı."),
            (13, "T3 Ön Kuluçka", "2025 Bahar", ParticipationStatus.Completed, null),
            (13, "T3 Kuluçka", "2025 Dönemi", ParticipationStatus.InProgress, null),
            (14, "T3 Kuluçka", "2024 Dönemi", ParticipationStatus.Graduated, null),
            (14, "TEKNOFEST", "TEKNOFEST 2024", ParticipationStatus.Completed, null),
            (15, "T3 Ön Kuluçka", "2025 Bahar", ParticipationStatus.Graduated, null),
            (15, "TEKNOFEST", "TEKNOFEST 2025", ParticipationStatus.Completed, null),
            (16, "T3 Ön Kuluçka", "2026 Bahar", ParticipationStatus.InProgress, null),
            (17, "T3 Hızlandırma", "2025 Kohort 1", ParticipationStatus.Graduated, null),
            (17, "TEKNOFEST", "TEKNOFEST 2024", ParticipationStatus.Completed, null),
            (18, "DENEYAP Türkiye", "2025 Atölye Dönemi", ParticipationStatus.InProgress, null),
            (19, "TEKNOFEST", "TEKNOFEST 2024", ParticipationStatus.Completed, null),
            (19, "TEKNOFEST", "TEKNOFEST 2025", ParticipationStatus.Completed, null),
            (20, "T3 Ön Kuluçka", "2026 Bahar", ParticipationStatus.InProgress, null),
            (21, "T3 Kuluçka", "2025 Dönemi", ParticipationStatus.InProgress, null),
            (22, "T3 Kuluçka", "2025 Dönemi", ParticipationStatus.InProgress, null),
            (22, "TEKNOFEST", "TEKNOFEST 2025", ParticipationStatus.Completed, null),
            (23, "T3 Ön Kuluçka", "2026 Bahar", ParticipationStatus.InProgress, null),
            (24, "T3 Hızlandırma", "2025 Kohort 1", ParticipationStatus.Graduated, null),
            (25, "DENEYAP Türkiye", "2025 Atölye Dönemi", ParticipationStatus.InProgress, null),
            (27, "T3 Kuluçka", "2024 Dönemi", ParticipationStatus.Graduated,
                "Programdan mezun oldu, sonrasında çıkış yaptı."),
            (28, "TEKNOFEST", "TEKNOFEST 2024", ParticipationStatus.Completed, null),
            (28, "TEKNOFEST", "TEKNOFEST 2025", ParticipationStatus.Completed, null)
        ];

        foreach (var row in plan)
        {
            var term = programs[row.Program].Terms.First(t => t.Name == row.Term);
            var closed = row.Status is ParticipationStatus.Completed
                or ParticipationStatus.Graduated or ParticipationStatus.Dropped;

            db.ProgramParticipations.Add(new ProgramParticipation
            {
                StartupId = startups[row.Startup].Id,
                ProgramTermId = term.Id,
                Status = row.Status,
                JoinedOn = term.StartsOn,
                LeftOn = closed ? term.EndsOn : null,
                Notes = row.Note
            });
        }
    }

    // --- Başarı kayıtları -------------------------------------------------

    private void SeedAchievements(Startup[] startups)
    {
        void Investment(int i, DateOnly on, InvestmentRoundType type,
            decimal amount, decimal? valuation, params string[] investors) =>
            db.Achievements.Add(new InvestmentRound
            {
                StartupId = startups[i].Id,
                OccurredOn = on,
                RoundType = type,
                Amount = amount,
                Valuation = valuation,
                InvestorNames = [.. investors],
                IsVerified = true
            });

        void Grant(int i, DateOnly on, GrantInstitution institution,
            decimal amount, string programName) =>
            db.Achievements.Add(new GrantRecord
            {
                StartupId = startups[i].Id,
                OccurredOn = on,
                Institution = institution,
                ProgramName = programName,
                Amount = amount,
                IsVerified = true
            });

        void Award(int i, DateOnly on, string name, string organization, int? rank) =>
            db.Achievements.Add(new AwardRecord
            {
                StartupId = startups[i].Id,
                OccurredOn = on,
                Name = name,
                Organization = organization,
                Rank = rank,
                IsVerified = true
            });

        void Revenue(int i, int year, decimal amount) =>
            db.Achievements.Add(new RevenueRecord
            {
                StartupId = startups[i].Id,
                OccurredOn = new DateOnly(year, 12, 31),
                FiscalYear = year,
                Amount = amount,
                IsVerified = true
            });

        void Export(int i, int year, decimal amount, params string[] countries) =>
            db.Achievements.Add(new ExportRecord
            {
                StartupId = startups[i].Id,
                OccurredOn = new DateOnly(year, 12, 31),
                FiscalYear = year,
                Amount = amount,
                TargetCountries = [.. countries],
                IsVerified = true
            });

        // Anadolu Robotik — kuluçkadan yatırıma giden örnek yolculuk
        Grant(0, new DateOnly(2022, 9, 14), GrantInstitution.Tubitak, 750_000m, "1512 BiGG");
        Investment(0, new DateOnly(2023, 7, 18), InvestmentRoundType.PreSeed,
            4_500_000m, 45_000_000m, "T3 Girişim Fonu", "Anadolu Melek Ağı");
        Revenue(0, 2023, 2_800_000m);
        Investment(0, new DateOnly(2025, 3, 26), InvestmentRoundType.Seed,
            22_000_000m, 180_000_000m, "Boğaziçi Ventures Fonu", "Sanayi Yatırım Ortaklığı");
        Revenue(0, 2024, 11_400_000m);
        Award(0, new DateOnly(2024, 10, 5), "Endüstriyel Robotik Ödülü", "TEKNOFEST", 1);

        // Kuzey Sensör — savunma odaklı, ihracata açılmış
        Grant(1, new DateOnly(2022, 2, 8), GrantInstitution.Tubitak, 1_200_000m, "1507 KOBİ Ar-Ge");
        Investment(1, new DateOnly(2024, 5, 30), InvestmentRoundType.SeriesA,
            65_000_000m, 520_000_000m, "Savunma Teknoloji Fonu");
        Revenue(1, 2024, 34_500_000m);
        Export(1, 2024, 9_800_000m, "Azerbaycan", "Katar");
        Revenue(1, 2025, 51_200_000m);

        // Marmara Biyoteknoloji — erken aşama, hibe ağırlıklı
        Grant(2, new DateOnly(2023, 6, 20), GrantInstitution.Tubitak, 900_000m, "1512 BiGG");
        Grant(2, new DateOnly(2024, 11, 12), GrantInstitution.EuropeanUnion,
            3_400_000m, "Horizon Europe EIC");
        Investment(2, new DateOnly(2025, 6, 11), InvestmentRoundType.Angel,
            2_750_000m, 38_000_000m, "Sağlık Melek Yatırımcıları");
        Award(2, new DateOnly(2025, 9, 20), "Sağlık Teknolojileri Kategorisi", "TEKNOFEST", 2);

        // Ege Tarım Verisi
        Grant(3, new DateOnly(2023, 3, 15), GrantInstitution.Kosgeb, 480_000m, "Ar-Ge İnovasyon");
        Investment(3, new DateOnly(2024, 9, 4), InvestmentRoundType.PreSeed,
            6_200_000m, 62_000_000m, "Tarım Teknoloji Fonu", "Ege Melek Ağı");
        Revenue(3, 2024, 4_100_000m);
        Revenue(3, 2025, 8_900_000m);

        // Selçuk Enerji — mezun, ihracat yapan
        Grant(4, new DateOnly(2021, 11, 22), GrantInstitution.Tubitak, 1_050_000m, "1501 Sanayi Ar-Ge");
        Investment(4, new DateOnly(2023, 2, 13), InvestmentRoundType.Seed,
            18_500_000m, 140_000_000m, "Enerji Dönüşüm Fonu");
        Revenue(4, 2023, 21_700_000m);
        Export(4, 2024, 14_300_000m, "Almanya", "Romanya", "Gürcistan");
        Revenue(4, 2024, 39_800_000m);

        // Karadeniz Görüntü İşleme — en erken aşama
        Grant(5, new DateOnly(2024, 4, 18), GrantInstitution.Kosgeb, 350_000m, "Girişimcilik Desteği");
        Award(5, new DateOnly(2025, 9, 21), "Yapay Zekâ Kategorisi", "TEKNOFEST", 3);

        // Toros Uzay
        Grant(6, new DateOnly(2021, 5, 6), GrantInstitution.Tubitak, 2_100_000m, "1511 Öncelikli Alanlar");
        Award(6, new DateOnly(2024, 10, 6), "Uydu Alt Sistemleri", "TEKNOFEST", 1);
        Investment(6, new DateOnly(2025, 8, 19), InvestmentRoundType.SeriesA,
            48_000_000m, 400_000_000m, "Uzay Teknolojileri Fonu");
        Revenue(6, 2024, 16_200_000m);

        // Boğaz Finans Teknolojileri
        Investment(7, new DateOnly(2023, 5, 9), InvestmentRoundType.Seed,
            27_000_000m, 210_000_000m, "Fintek Girişim Fonu", "Global Melek Ağı");
        Revenue(7, 2024, 29_400_000m);
        Investment(7, new DateOnly(2025, 10, 2), InvestmentRoundType.SeriesA,
            95_000_000m, 850_000_000m, "Uluslararası Büyüme Fonu");

        // Kapadokya Eğitim Teknolojileri
        Grant(8, new DateOnly(2024, 2, 27), GrantInstitution.Ministry, 620_000m, "Eğitim Teknolojileri");
        Revenue(8, 2025, 3_300_000m);

        // Trakya Lojistik Zekâsı
        Grant(9, new DateOnly(2022, 7, 11), GrantInstitution.DevelopmentAgency,
            540_000m, "Bölgesel Kalkınma");
        Investment(9, new DateOnly(2024, 12, 5), InvestmentRoundType.PreSeed,
            5_400_000m, 54_000_000m, "Lojistik Yatırım Ağı");
        Revenue(9, 2024, 7_600_000m);
        Award(9, new DateOnly(2025, 9, 20), "Akıllı Ulaşım", "TEKNOFEST", 2);

        // Fırat Malzeme Bilimi
        Grant(10, new DateOnly(2021, 4, 29), GrantInstitution.Tubitak, 3_200_000m, "1004 Mükemmeliyet");
        Award(10, new DateOnly(2024, 10, 6), "İleri Malzeme", "TEKNOFEST", 1);
        Export(10, 2025, 6_700_000m, "İtalya", "Polonya");
        Revenue(10, 2025, 12_100_000m);

        // Van Su Teknolojileri — en yeni girişim
        Grant(11, new DateOnly(2024, 8, 14), GrantInstitution.Kosgeb, 410_000m, "Girişimcilik Desteği");

        // --- Faz 5 demo ölçeği ------------------------------------------
        // Panodaki dağılımların gerçekçi görünmesi için: her olgunluk
        // seviyesinden örnek, yatırım almamış girişimler ve yalnızca hibeyle
        // ilerleyenler bilinçli olarak karışık bırakıldı.

        // Sakarya Batarya — hızlandırmadan geçmiş, ihracata açılmış
        Grant(12, new DateOnly(2021, 10, 4), GrantInstitution.Tubitak, 1_800_000m, "1507 KOBİ Ar-Ge");
        Investment(12, new DateOnly(2023, 11, 21), InvestmentRoundType.Seed,
            31_000_000m, 240_000_000m, "Mobilite Yatırım Fonu");
        Revenue(12, 2024, 27_300_000m);
        Export(12, 2025, 11_500_000m, "Bulgaristan", "Sırbistan");
        Revenue(12, 2025, 44_600_000m);

        // Gaziantep Gıda Teknolojileri
        Grant(13, new DateOnly(2022, 12, 6), GrantInstitution.Kosgeb, 520_000m, "Ar-Ge İnovasyon");
        Investment(13, new DateOnly(2025, 2, 18), InvestmentRoundType.PreSeed,
            7_800_000m, 68_000_000m, "Gıda Teknolojileri Ağı");
        Revenue(13, 2025, 5_200_000m);

        // Bursa Otonom Sürüş — mezun, en büyük turlardan biri
        Grant(14, new DateOnly(2021, 2, 17), GrantInstitution.Tubitak, 2_600_000m, "1511 Öncelikli Alanlar");
        Investment(14, new DateOnly(2022, 8, 8), InvestmentRoundType.Seed,
            24_000_000m, 190_000_000m, "Otomotiv Girişim Fonu");
        Investment(14, new DateOnly(2025, 1, 29), InvestmentRoundType.SeriesB,
            160_000_000m, 1_250_000_000m, "Uluslararası Mobilite Fonu", "Sanayi Yatırım Ortaklığı");
        Revenue(14, 2024, 58_700_000m);
        Award(14, new DateOnly(2024, 10, 6), "Otonom Araç Kategorisi", "TEKNOFEST", 2);

        // Eskişehir Havacılık Yazılımı
        Grant(15, new DateOnly(2023, 4, 26), GrantInstitution.Tubitak, 1_350_000m, "1501 Sanayi Ar-Ge");
        Award(15, new DateOnly(2025, 9, 21), "Havacılık Yazılımı", "TEKNOFEST", 1);
        Revenue(15, 2025, 6_400_000m);

        // Antalya Turizm Verisi — yalnızca ciro, yatırımsız
        Revenue(16, 2024, 1_900_000m);
        Revenue(16, 2025, 4_300_000m);

        // Kayseri Savunma Elektroniği — en olgun savunma girişimi
        Grant(17, new DateOnly(2020, 3, 11), GrantInstitution.Tubitak, 4_100_000m, "1004 Mükemmeliyet");
        Investment(17, new DateOnly(2022, 6, 15), InvestmentRoundType.SeriesA,
            72_000_000m, 610_000_000m, "Savunma Teknoloji Fonu");
        Revenue(17, 2024, 96_500_000m);
        Export(17, 2024, 23_400_000m, "Azerbaycan", "Pakistan", "Katar");
        Revenue(17, 2025, 118_200_000m);
        Award(17, new DateOnly(2024, 10, 5), "Elektronik Harp", "TEKNOFEST", 1);

        // Denizli Tekstil Zekâsı
        Grant(18, new DateOnly(2023, 9, 8), GrantInstitution.DevelopmentAgency, 460_000m, "Bölgesel Kalkınma");
        Revenue(18, 2025, 2_700_000m);

        // Samsun Deniz Teknolojileri
        Grant(19, new DateOnly(2022, 5, 19), GrantInstitution.Tubitak, 1_150_000m, "1512 BiGG");
        Award(19, new DateOnly(2024, 10, 6), "İnsansız Deniz Aracı", "TEKNOFEST", 3);
        Investment(19, new DateOnly(2025, 5, 7), InvestmentRoundType.Angel,
            3_600_000m, 42_000_000m, "Karadeniz Melek Ağı");

        // Malatya Tarım Robotları — en erken aşama
        Grant(20, new DateOnly(2024, 6, 27), GrantInstitution.Kosgeb, 380_000m, "Girişimcilik Desteği");

        // Diyarbakır Güneş Enerjisi
        Grant(21, new DateOnly(2022, 10, 13), GrantInstitution.Ministry, 890_000m, "Yenilenebilir Enerji");
        Investment(21, new DateOnly(2024, 7, 23), InvestmentRoundType.PreSeed,
            5_900_000m, 51_000_000m, "Güneydoğu Yatırım Ağı");
        Revenue(21, 2025, 9_800_000m);

        // İzmir Sağlık Yapay Zekâsı
        Grant(22, new DateOnly(2023, 3, 30), GrantInstitution.EuropeanUnion, 2_900_000m, "Horizon Europe EIC");
        Investment(22, new DateOnly(2025, 4, 14), InvestmentRoundType.Seed,
            19_500_000m, 165_000_000m, "Sağlık Teknolojileri Fonu", "Ege Melek Ağı");
        Award(22, new DateOnly(2025, 9, 20), "Sağlıkta Yapay Zekâ", "TEKNOFEST", 1);

        // Konya Hidrojen — yalnızca hibe
        Grant(23, new DateOnly(2024, 3, 5), GrantInstitution.Tubitak, 1_600_000m, "1511 Öncelikli Alanlar");

        // Ankara Siber Kalkan
        Investment(24, new DateOnly(2023, 9, 12), InvestmentRoundType.Seed,
            16_800_000m, 130_000_000m, "Siber Güvenlik Fonu");
        Revenue(24, 2024, 12_600_000m);
        Export(24, 2025, 4_900_000m, "Hollanda");
        Revenue(24, 2025, 21_400_000m);

        // Mersin Liman Otomasyonu
        Grant(25, new DateOnly(2023, 1, 24), GrantInstitution.DevelopmentAgency, 640_000m, "Lojistik Altyapı");
        Revenue(25, 2025, 7_100_000m);

        // Erzurum Soğuk İklim — programa girmemiş, kaydı yeni
        Grant(26, new DateOnly(2024, 11, 6), GrantInstitution.Kosgeb, 320_000m, "Girişimcilik Desteği");

        // Adana Su Yönetimi — çıkış yapmış girişim
        Grant(27, new DateOnly(2021, 9, 2), GrantInstitution.Tubitak, 980_000m, "1507 KOBİ Ar-Ge");
        Investment(27, new DateOnly(2023, 4, 19), InvestmentRoundType.Seed,
            14_200_000m, 105_000_000m, "Altyapı Girişim Fonu");
        Revenue(27, 2024, 18_300_000m);

        // Kocaeli Kompozit
        Grant(28, new DateOnly(2021, 6, 8), GrantInstitution.Tubitak, 2_400_000m, "1501 Sanayi Ar-Ge");
        Revenue(28, 2024, 33_900_000m);
        Export(28, 2025, 8_200_000m, "Almanya", "Çekya");
        Award(28, new DateOnly(2025, 9, 21), "İleri Kompozit", "TEKNOFEST", 2);

        // Rize Çay Teknolojisi — programa girmemiş
        Revenue(29, 2025, 1_400_000m);

        // Şanlıurfa Kuraklık Verisi — henüz kaydı yok, pano "veri yok" göstersin
        // (bilinçli olarak boş bırakıldı)

        // İstanbul Kuantum Yazılımı
        Grant(31, new DateOnly(2025, 1, 30), GrantInstitution.Tubitak, 1_100_000m, "1512 BiGG");
    }

    // --- Kilometre taşları ------------------------------------------------

    private void SeedMilestones(Startup[] startups)
    {
        // Yerel fonksiyon adı bilinçli olarak "Milestone" değil: aynı addaki
        // varlık tipini gölgeleyip `new Milestone { … }` ifadesini bozardı.
        void Step(int i, DateOnly on, MilestoneType type, string title, string? description) =>
            db.Milestones.Add(new Milestone
            {
                StartupId = startups[i].Id,
                OccurredOn = on,
                Type = type,
                Title = title,
                Description = description
            });

        Step(0, new DateOnly(2023, 11, 8), MilestoneType.ProductLaunch,
            "İlk seri üretim robot kolu sahaya çıktı",
            "Bursa'daki otomotiv yan sanayi tesisinde iki hat devreye alındı.");
        Step(0, new DateOnly(2025, 5, 20), MilestoneType.TeamGrowth,
            "Ekip 24 kişiye ulaştı", null);

        Step(1, new DateOnly(2023, 9, 4), MilestoneType.Certification,
            "AS9100 havacılık kalite belgesi alındı", null);
        Step(1, new DateOnly(2025, 1, 16), MilestoneType.Partnership,
            "Ana yükleniciyle tedarik anlaşması imzalandı", null);

        Step(2, new DateOnly(2024, 7, 2), MilestoneType.Certification,
            "CE-IVD işaretlemesi tamamlandı", null);

        Step(3, new DateOnly(2024, 4, 22), MilestoneType.Partnership,
            "Tarım kredi kooperatifleriyle pilot iş birliği", "12 ilde 400 parselde saha denemesi.");

        Step(4, new DateOnly(2023, 6, 9), MilestoneType.OfficeOpening,
            "Konya OSB'de üretim tesisi açıldı", null);
        Step(4, new DateOnly(2024, 3, 18), MilestoneType.Pivot,
            "Konut segmentinden ticari ölçeğe geçildi",
            "Ürün hattı yalnızca ticari santrallere odaklandı.");

        Step(6, new DateOnly(2023, 12, 14), MilestoneType.Partnership,
            "Yer istasyonu ağı iş birliği", null);

        Step(7, new DateOnly(2024, 6, 27), MilestoneType.Certification,
            "BDDK açık bankacılık uyum onayı", null);

        Step(9, new DateOnly(2023, 10, 30), MilestoneType.ProductLaunch,
            "Soğuk zincir izleme modülü yayına alındı", null);

        Step(10, new DateOnly(2025, 2, 11), MilestoneType.OfficeOpening,
            "Elazığ'da pilot üretim hattı kuruldu", null);
    }

    // --- Kullanıcılar -----------------------------------------------------

    /// <summary>
    /// Tohumlanan hesaplar. Onay kuyruğu tohumu göndereni ve karar vereni
    /// bilmek zorunda, bu yüzden kullanıcılar geri döndürülüyor.
    /// </summary>
    private sealed record SeededUsers(
        User Admin,
        User IncubationManager,
        User FestivalManager,
        User DecisionMaker,
        Dictionary<int, User> PortalUsers);

    private SeededUsers SeedUsers(
        Startup[] startups,
        Dictionary<string, EcosystemProgram> programs,
        string password)
    {
        var hash = passwordHasher.Hash(password);

        User Create(string localPart, string fullName, UserRole role, Guid? startupId = null) =>
            new()
            {
                Email = $"{localPart}@{SeedCatalog.EmailDomain}",
                FullName = fullName,
                Role = role,
                PasswordHash = hash,
                StartupId = startupId,
                IsActive = true
            };

        var admin = Create("admin", "Sistem Yöneticisi", UserRole.SuperAdmin);

        var incubationManager = Create(
            "kulucka.yoneticisi", "Nurcan Bilge", UserRole.ProgramManager);

        var festivalManager = Create(
            "teknofest.yoneticisi", "Serkan Duman", UserRole.ProgramManager);

        var decisionMaker = Create("karar.verici", "Mütevelli Üyesi", UserRole.DecisionMaker);

        // Portal hesapları iki yöneticinin kapsamına bilinçli olarak dağıtılır:
        // 0 ve 2 Kuluçka programlarında, 6 ve 9 TEKNOFEST/Hızlandırma'da.
        // Böylece onay kuyruğunun kapsam daraltması demoda kanıtlanabilir —
        // her yönetici yalnızca kendi programındaki önerileri görür.
        Dictionary<int, User> portalUsers = new()
        {
            [0] = Create("girisim", "Elif Yıldırım", UserRole.StartupUser, startups[0].Id),
            [2] = Create("girisim.marmara", "Zeynep Aksoy", UserRole.StartupUser, startups[2].Id),
            [6] = Create("girisim.toros", "Kemal Doğan", UserRole.StartupUser, startups[6].Id),
            [9] = Create("girisim.trakya", "Cem Öztürk", UserRole.StartupUser, startups[9].Id)
        };

        db.Users.AddRange(admin, incubationManager, festivalManager, decisionMaker);
        db.Users.AddRange(portalUsers.Values);

        // İki yönetici kasıtlı olarak ayrık programlara atanır; kapsam
        // daraltmasının çalıştığı demoda tek bakışta görülür.
        Assign(incubationManager, "T3 Ön Kuluçka", "T3 Kuluçka");
        Assign(festivalManager, "TEKNOFEST", "DENEYAP Türkiye", "T3 Hızlandırma");

        void Assign(User user, params string[] programNames)
        {
            foreach (var name in programNames)
                db.UserProgramAssignments.Add(new UserProgramAssignment
                {
                    UserId = user.Id,
                    ProgramId = programs[name].Id,
                    AssignedAt = DateTimeOffset.UtcNow
                });
        }

        return new SeededUsers(
            admin, incubationManager, festivalManager, decisionMaker, portalUsers);
    }

    // --- Onay kuyruğu -----------------------------------------------------

    /// <summary>
    /// Onay akışının demo verisi. Boş bir kuyruk MVP #3'ü anlatamaz: ekranda
    /// hem bekleyen öneriler, hem karara bağlanmış geçmiş, hem de girişimin ret
    /// gerekçesini gördüğü bir kayıt bulunmak zorunda.
    ///
    /// Öneriler kasıtlı olarak iki yöneticinin kapsamına bölünmüş durumda ve
    /// biri vergi kimlik numarasını değiştiriyor — Program Yöneticisi o satırda
    /// "değişti" ibaresini görür, değeri görmez. Maskelemenin onay ekranında da
    /// çalıştığı tek bakışta gösterilebilsin.
    /// </summary>
    private void SeedChangeRequests(Startup[] startups, SeededUsers users)
    {
        var now = DateTimeOffset.UtcNow;

        void Profile(
            int startupIndex,
            int daysAgo,
            Func<StartupWriteModel, StartupWriteModel> change,
            ChangeRequestStatus status = ChangeRequestStatus.Pending,
            User? reviewer = null,
            string? note = null,
            int reviewedDaysAgo = 0)
        {
            var startup = startups[startupIndex];
            var before = StartupWriteModel.From(startup);

            Add(new ChangeRequest
            {
                StartupId = startup.Id,
                SubmittedByUserId = users.PortalUsers[startupIndex].Id,
                SubmittedAt = now.AddDays(-daysAgo),
                TargetType = ChangeTargetType.Startup,
                Operation = ChangeOperation.Update,
                BeforeJson = ChangeRequestJson.Serialize(before),
                PayloadJson = ChangeRequestJson.Serialize(change(before))
            }, status, reviewer, note, reviewedDaysAgo);
        }

        void NewMember(int startupIndex, int daysAgo, TeamMemberWriteModel member) =>
            Add(new ChangeRequest
            {
                StartupId = startups[startupIndex].Id,
                SubmittedByUserId = users.PortalUsers[startupIndex].Id,
                SubmittedAt = now.AddDays(-daysAgo),
                TargetType = ChangeTargetType.TeamMember,
                Operation = ChangeOperation.Create,
                PayloadJson = ChangeRequestJson.Serialize(member)
            }, ChangeRequestStatus.Pending, null, null, 0);

        void NewAchievement(int startupIndex, int daysAgo, AchievementWriteModel model) =>
            Add(new ChangeRequest
            {
                StartupId = startups[startupIndex].Id,
                SubmittedByUserId = users.PortalUsers[startupIndex].Id,
                SubmittedAt = now.AddDays(-daysAgo),
                TargetType = ChangeTargetType.Achievement,
                Operation = ChangeOperation.Create,
                PayloadJson = ChangeRequestJson.Serialize(model)
            }, ChangeRequestStatus.Pending, null, null, 0);

        void MemberChange(
            int startupIndex, int daysAgo, string fullName, ChangeOperation operation,
            Func<TeamMemberWriteModel, TeamMemberWriteModel>? change = null)
        {
            var startup = startups[startupIndex];
            var member = startup.TeamMembers.First(m => m.FullName == fullName);
            var before = TeamMemberWriteModel.From(member);

            Add(new ChangeRequest
            {
                StartupId = startup.Id,
                SubmittedByUserId = users.PortalUsers[startupIndex].Id,
                SubmittedAt = now.AddDays(-daysAgo),
                TargetType = ChangeTargetType.TeamMember,
                TargetId = member.Id,
                Operation = operation,
                BeforeJson = ChangeRequestJson.Serialize(before),
                // Çıkarma önerisinde taşınacak yeni değer yok.
                PayloadJson = change is null
                    ? ChangeRequestJson.EmptyPayload
                    : ChangeRequestJson.Serialize(change(before))
            }, ChangeRequestStatus.Pending, null, null, 0);
        }

        void Add(
            ChangeRequest request, ChangeRequestStatus status,
            User? reviewer, string? note, int reviewedDaysAgo)
        {
            request.Status = status;

            if (status != ChangeRequestStatus.Pending)
            {
                request.ReviewedByUserId = reviewer?.Id;
                request.ReviewedAt = now.AddDays(-reviewedDaysAgo);
                request.ReviewNote = note;
            }

            db.ChangeRequests.Add(request);
        }

        // --- Bekleyen öneriler: Kuluçka kapsamı (Nurcan Bilge görür) --------

        Profile(0, daysAgo: 3, before => before with
        {
            ProductDescription = before.ProductDescription
                + " 2026 sürümünde kuvvet geri beslemeli kavrama modülü eklendi.",
            Website = "https://www.anadolurobotik.test",
            ContactPhone = "+90 500 000 01 41"
        });

        NewMember(0, daysAgo: 1, new TeamMemberWriteModel(
            "Burak Aslan", "Kıdemli Mekanik Tasarım Mühendisi",
            "burak.aslan@anadolurobotik.test", "+90 500 001 01 04",
            "https://www.linkedin.test/in/burak.aslan",
            IsFounder: false, JoinedOn: new DateOnly(2026, 6, 1)));

        // Vergi numarası değişikliği: maskelemenin onay ekranındaki kanıtı.
        Profile(2, daysAgo: 6, before => before with
        {
            TaxNumber = "1234567899",
            LegalName = "Marmara Biyoteknoloji Ar-Ge A.Ş.",
            ContactEmail = "kurumsal@marmarabiyo.test"
        });

        MemberChange(2, daysAgo: 2, "Selin Bozkurt", ChangeOperation.Update,
            before => before with
            {
                Title = "Kıdemli Biyoinformatik Uzmanı",
                Phone = "+90 500 001 03 09"
            });

        // --- Bekleyen öneriler: TEKNOFEST kapsamı (Serkan Duman görür) ------

        Profile(6, daysAgo: 4, before => before with
        {
            City = "Ankara",
            ProductDescription = before.ProductDescription
                + " Uydu alt sistemleri için nitelik testleri tamamlandı."
        });

        MemberChange(9, daysAgo: 5, "Ebru Kılıç", ChangeOperation.Delete);

        // --- Bekleyen finansal öneriler (MVP #4) ---------------------------
        // Girişim kendi cirosunu ve ödülünü öneriyor; kayıt ancak yetkili
        // onayladıktan sonra "doğrulanmış" sayılıyor. Kuyrukta hem tutarlı
        // hem tutarsız tür bulunsun diye biri ciro, diğeri ödül.

        NewAchievement(0, daysAgo: 2, new AchievementWriteModel(
            Kind: AchievementKind.Revenue,
            OccurredOn: new DateOnly(2025, 12, 31),
            Note: "Bağımsız denetim raporu hazırlanıyor.",
            Amount: 18_400_000m, Currency: "TRY",
            FiscalYear: 2025, Quarter: null,
            RoundType: null, Valuation: null, InvestorNames: null,
            Institution: null, ProgramName: null,
            AwardName: null, Organization: null, Rank: null,
            TargetCountries: null));

        NewAchievement(6, daysAgo: 1, new AchievementWriteModel(
            Kind: AchievementKind.Award,
            OccurredOn: new DateOnly(2026, 5, 9),
            Note: null,
            Amount: null, Currency: null,
            FiscalYear: null, Quarter: null,
            RoundType: null, Valuation: null, InvestorNames: null,
            Institution: null, ProgramName: null,
            AwardName: "Uzay Teknolojileri Yarışması", Organization: "TEKNOFEST", Rank: 2,
            TargetCountries: null));

        // --- Karara bağlanmış geçmiş: portalın "önerim ne oldu" ekranı ------

        Profile(0, daysAgo: 12,
            before => before with { City = "Ankara" },
            ChangeRequestStatus.Approved,
            users.IncubationManager,
            "Bilgiler ticaret sicil kaydıyla doğrulandı, yayına alındı.",
            reviewedDaysAgo: 10);

        Profile(0, daysAgo: 9,
            before => before with { TaxNumber = "9999999999" },
            ChangeRequestStatus.Rejected,
            users.Admin,
            "Vergi kimlik numarası değişikliği için ticaret sicil gazetesi gerekiyor. "
            + "Belgeyi yükledikten sonra öneriyi tekrar gönderin.",
            reviewedDaysAgo: 8);
    }

    // --- Dokümanlar -------------------------------------------------------

    /// <summary>
    /// Demo dokümanları (MVP #4). Dosyalar gerçekten depoya yazılıyor: indirme
    /// yolunun çalıştığı ancak indirilen dosya açıldığında görülebiliyor.
    ///
    /// Biri onay bekleyen yükleme olarak duruyor — girişim kullanıcısının
    /// yüklediği dosya listeye girmeden kuyrukta bekliyor, MVP #3 ile #4'ün
    /// kesiştiği yer demoda tek ekranda görünsün.
    /// </summary>
    private async Task SeedDocumentsAsync(
        Startup[] startups, SeededUsers users, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        async Task<string> WriteAsync(string fileName, string contentType, byte[] content)
        {
            using var stream = new MemoryStream(content);
            return await storage.SaveAsync(stream, fileName, contentType, ct);
        }

        async Task Publish(
            int startupIndex, User uploader, DocumentType type,
            string fileName, string contentType, byte[] content, int daysAgo)
        {
            db.Documents.Add(new Document
            {
                StartupId = startups[startupIndex].Id,
                Type = type,
                FileName = fileName,
                StoragePath = await WriteAsync(fileName, contentType, content),
                ContentType = contentType,
                SizeBytes = content.Length,
                UploadedByUserId = uploader.Id,
                UploadedAt = now.AddDays(-daysAgo)
            });
        }

        const string Pdf = "application/pdf";
        const string Csv = "text/csv";

        await Publish(0, users.IncubationManager, DocumentType.PitchDeck,
            "anadolu_robotik_yatirimci_sunumu.pdf", Pdf,
            SeedDocumentFiles.Pdf(
                "Anadolu Robotik - Yatirimci Sunumu",
                "Kurgu demo belgesi. T3 Girisim Ekosistemi Yonetim Sistemi.",
                "Urun: kuvvet geri beslemeli endustriyel robot kolu.",
                "Bu dosya yalnizca demo amacli uretilmistir."),
            daysAgo: 40);

        await Publish(0, users.IncubationManager, DocumentType.Financials,
            "anadolu_robotik_2024_gelir_tablosu.csv", Csv,
            SeedDocumentFiles.Csv(
                "Kalem;2023;2024",
                "Net satışlar;2800000;9600000",
                "Satışların maliyeti;1450000;4900000",
                "Ar-Ge gideri;780000;2100000",
                "Faaliyet kârı;120000;1350000"),
            daysAgo: 30);

        await Publish(2, users.IncubationManager, DocumentType.Incorporation,
            "marmara_biyoteknoloji_kurulus_belgesi.pdf", Pdf,
            SeedDocumentFiles.Pdf(
                "Marmara Biyoteknoloji - Kurulus Belgesi",
                "Kurgu demo belgesi; gercek bir ticaret sicil kaydi degildir.",
                "Kurulus yili: 2022."),
            daysAgo: 60);

        await Publish(6, users.FestivalManager, DocumentType.Patent,
            "toros_uydu_alt_sistem_patent_ozeti.pdf", Pdf,
            SeedDocumentFiles.Pdf(
                "Toros Uzay - Patent Ozeti",
                "Kurgu demo belgesi.",
                "Bulus: kucuk uydular icin isil kontrol modulu."),
            daysAgo: 22);

        await Publish(9, users.FestivalManager, DocumentType.Report,
            "trakya_enerji_saha_test_raporu.pdf", Pdf,
            SeedDocumentFiles.Pdf(
                "Trakya Enerji - Saha Test Raporu",
                "Kurgu demo belgesi.",
                "Saha testi: 12 haftalik dayanim olcumu."),
            daysAgo: 15);

        // Onay bekleyen yükleme: dosya depoda, kayıt henüz yok.
        var pendingFile = SeedDocumentFiles.Pdf(
            "Anadolu Robotik - 2025 Faaliyet Raporu",
            "Kurgu demo belgesi.",
            "Bu dosya onay bekliyor; onaylanana kadar dokuman listesinde gorunmez.");

        var pendingName = "anadolu_robotik_2025_faaliyet_raporu.pdf";

        var proposal = new DocumentProposalModel(
            DocumentType.Report,
            pendingName,
            await WriteAsync(pendingName, Pdf, pendingFile),
            Pdf,
            pendingFile.Length);

        db.ChangeRequests.Add(new ChangeRequest
        {
            StartupId = startups[0].Id,
            SubmittedByUserId = users.PortalUsers[0].Id,
            SubmittedAt = now.AddDays(-1),
            TargetType = ChangeTargetType.Document,
            Operation = ChangeOperation.Create,
            PayloadJson = ChangeRequestJson.Serialize(proposal),
            Status = ChangeRequestStatus.Pending
        });
    }

    /// <summary>
    /// Türkçe karakterleri ASCII'ye indirger. Harf eşlemesi küçültmeden ÖNCE
    /// yapılır: 'İ' harfi invariant kültürde birleşik noktaya dönüşüp eşlemeyi
    /// kaçırır.
    /// </summary>
    private static string Slug(string value)
    {
        var chars = value
            .Select(c => TurkishToAscii.GetValueOrDefault(c, c))
            .Select(char.ToLowerInvariant)
            .Where(c => char.IsAsciiLetterOrDigit(c) || c is ' ')
            .Select(c => c == ' ' ? '.' : c);

        return string.Concat(chars);
    }

    private static readonly Dictionary<char, char> TurkishToAscii = new()
    {
        ['ç'] = 'c', ['ğ'] = 'g', ['ı'] = 'i', ['ö'] = 'o', ['ş'] = 's', ['ü'] = 'u',
        ['Ç'] = 'C', ['Ğ'] = 'G', ['İ'] = 'I', ['Ö'] = 'O', ['Ş'] = 'S', ['Ü'] = 'U'
    };
}
