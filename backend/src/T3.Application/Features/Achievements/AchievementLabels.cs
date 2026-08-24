using T3.Domain.Achievements;

namespace T3.Application.Features.Achievements;

/// <summary>
/// Başarı/finans kayıtlarının Türkçe etiketleri. Zaman çizelgesi de bu tabloyu
/// kullanıyor: aynı yatırım turunun kartta "Seri A", çizelgede "SeriesA"
/// görünmesi kullanıcıya iki farklı sistem izlenimi verirdi.
/// </summary>
public static class AchievementLabels
{
    public static string Kind(AchievementKind kind) => kind switch
    {
        AchievementKind.Revenue => "Ciro",
        AchievementKind.Export => "İhracat",
        AchievementKind.Investment => "Yatırım turu",
        AchievementKind.Grant => "Hibe / destek",
        AchievementKind.Award => "Ödül",
        _ => kind.ToString()
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

    /// <summary>Dönem etiketi: çeyrek verilmediyse yıl tek başına yazılır.</summary>
    public static string Period(int fiscalYear, int? quarter) =>
        quarter is { } q ? $"{fiscalYear} Ç{q}" : $"{fiscalYear}";
}
