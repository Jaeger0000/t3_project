using T3.Domain.Common;

namespace T3.Domain.Programs;

/// <summary>Bir programın belirli dönemi ("2025 Bahar", "TEKNOFEST 2025").</summary>
public class ProgramTerm : Entity, IAuditable, ISoftDelete
{
    public Guid ProgramId { get; set; }
    public EcosystemProgram Program { get; set; } = null!;

    public string Name { get; set; } = null!;
    public DateOnly StartsOn { get; set; }
    public DateOnly? EndsOn { get; set; }

    public ICollection<ProgramParticipation> Participations { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>Program pasife alındığında birlikte işaretlenir.</summary>
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
