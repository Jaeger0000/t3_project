namespace T3.Domain.Achievements;

public enum InvestmentRoundType
{
    Other = 0,
    Angel = 1,
    PreSeed = 2,
    Seed = 3,
    SeriesA = 4,
    SeriesB = 5,
    SeriesC = 6,
    Debt = 7
}

/// <summary>Alınan yatırım turu.</summary>
public class InvestmentRound : MoneyAchievement
{
    public InvestmentRoundType RoundType { get; set; }

    /// <summary>Tur sonrası değerleme; bilinmiyorsa null.</summary>
    public decimal? Valuation { get; set; }

    /// <summary>Tura katılan yatırımcılar (Postgres text[]).</summary>
    public List<string> InvestorNames { get; set; } = [];
}
