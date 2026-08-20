namespace T3.Domain.Achievements;

/// <summary>Dönemsel ihracat kaydı.</summary>
public class ExportRecord : PeriodicMoneyAchievement
{
    /// <summary>İhracat yapılan ülkeler (Postgres text[]).</summary>
    public List<string> TargetCountries { get; set; } = [];
}
