namespace T3.Domain.Achievements;

public enum GrantInstitution
{
    Other = 0,
    Tubitak = 1,
    Kosgeb = 2,
    Teknofest = 3,
    EuropeanUnion = 4,
    Ministry = 5,
    DevelopmentAgency = 6
}

/// <summary>Alınan hibe / destek.</summary>
public class GrantRecord : MoneyAchievement
{
    public GrantInstitution Institution { get; set; }
    public string? ProgramName { get; set; }
}
