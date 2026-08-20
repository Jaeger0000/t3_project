namespace T3.Domain.Achievements;

/// <summary>Kazanılan ödül / derece. Tutar taşımadığı için doğrudan Achievement'tan türer.</summary>
public class AwardRecord : Achievement
{
    public string Name { get; set; } = null!;
    public string? Organization { get; set; }

    /// <summary>Derece (1, 2, 3 …); derece yoksa null.</summary>
    public int? Rank { get; set; }
}
