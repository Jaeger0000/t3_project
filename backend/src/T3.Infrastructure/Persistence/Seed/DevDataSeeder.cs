using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using T3.Application.Common.Interfaces;
using T3.Domain.Achievements;
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
        SeedUsers(startups, programs, password);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Tohum veri yüklendi: {Programs} program, {Startups} girişim, {Users} kullanıcı.",
            programs.Count, startups.Length, await db.Users.CountAsync(ct));
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
    /// İlk altı girişim Ön Kuluçka/Kuluçka'ya, kalanlar TEKNOFEST, DENEYAP ve
    /// Hızlandırma'ya bağlanır. Bölünme kasıtlı: iki farklı Program Yöneticisi
    /// hesabı demoda birbirinden tamamen farklı girişim listesi görür.
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
            (11, "DENEYAP Türkiye", "2025 Atölye Dönemi", ParticipationStatus.InProgress, null)
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

    private void SeedUsers(
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

        var startupUser = Create(
            "girisim", "Elif Yıldırım", UserRole.StartupUser, startups[0].Id);

        db.Users.AddRange(admin, incubationManager, festivalManager, decisionMaker, startupUser);

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
