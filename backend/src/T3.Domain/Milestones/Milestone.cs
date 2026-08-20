using T3.Domain.Common;
using T3.Domain.Startups;

namespace T3.Domain.Milestones;

public enum MilestoneType
{
    Other = 0,
    ProductLaunch = 1,
    TeamGrowth = 2,
    Partnership = 3,
    Pivot = 4,
    Certification = 5,
    OfficeOpening = 6
}

/// <summary>
/// Elle girilen gelişim adımı. Program katılımları ve başarı kayıtları zaten
/// kendi tablolarında durduğu için burada yalnızca onlara girmeyen adımlar
/// tutulur; zaman çizelgesi sorgu anında üçünün birleşimiyle üretilir.
/// </summary>
public class Milestone : Entity, IAuditable, ISoftDelete
{
    public Guid StartupId { get; set; }
    public Startup Startup { get; set; } = null!;

    public MilestoneType Type { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public DateOnly OccurredOn { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>Girişim pasife alındığında birlikte işaretlenir.</summary>
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
