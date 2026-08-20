using T3.Domain.Achievements;
using T3.Domain.Milestones;
using T3.Domain.Programs;

namespace T3.Application.Features.Startups.GetStartupTimeline;

/// <summary>
/// Çizelge anlatısında kullanılan Türkçe etiketler. Yalnızca bu dilim
/// kullanıyor; filtre açılırlarının etiketleri arayüzde durur, bu yüzden
/// iki yerde aynı tabloyu taşımıyoruz.
/// </summary>
internal static class TimelineLabels
{
    public static string Participation(ParticipationStatus status) => status switch
    {
        ParticipationStatus.Applied => "Başvurdu",
        ParticipationStatus.Accepted => "Kabul edildi",
        ParticipationStatus.InProgress => "Devam ediyor",
        ParticipationStatus.Completed => "Tamamlandı",
        ParticipationStatus.Graduated => "Mezun oldu",
        ParticipationStatus.Dropped => "Ayrıldı",
        _ => status.ToString()
    };

    public static string InvestmentRound(InvestmentRoundType type) => type switch
    {
        InvestmentRoundType.Angel => "Melek yatırım",
        InvestmentRoundType.PreSeed => "Pre-Seed",
        InvestmentRoundType.Seed => "Seed",
        InvestmentRoundType.SeriesA => "Seri A",
        InvestmentRoundType.SeriesB => "Seri B",
        InvestmentRoundType.SeriesC => "Seri C",
        InvestmentRoundType.Debt => "Borç finansmanı",
        _ => "Yatırım"
    };

    public static string Institution(GrantInstitution institution) => institution switch
    {
        GrantInstitution.Tubitak => "TÜBİTAK",
        GrantInstitution.Kosgeb => "KOSGEB",
        GrantInstitution.Teknofest => "TEKNOFEST",
        GrantInstitution.EuropeanUnion => "Avrupa Birliği",
        GrantInstitution.Ministry => "Bakanlık",
        GrantInstitution.DevelopmentAgency => "Kalkınma Ajansı",
        _ => "Diğer kurum"
    };

    public static string Milestone(MilestoneType type) => type switch
    {
        MilestoneType.ProductLaunch => "Ürün lansmanı",
        MilestoneType.TeamGrowth => "Ekip büyümesi",
        MilestoneType.Partnership => "İş birliği",
        MilestoneType.Pivot => "Pivot",
        MilestoneType.Certification => "Sertifikasyon",
        MilestoneType.OfficeOpening => "Ofis açılışı",
        _ => "Gelişim adımı"
    };

    public static string Period(int fiscalYear, int? quarter) =>
        quarter is { } q ? $"{fiscalYear} Ç{q}" : $"{fiscalYear}";
}
