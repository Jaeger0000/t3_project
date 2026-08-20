using T3.Domain.Common;
using T3.Domain.Startups;

namespace T3.Domain.Programs;

/// <summary>
/// Girişimin bir program dönemine katılımı (MVP #2). Gelişim yolculuğu
/// zaman çizelgesi bu kayıtların üzerine kurulur.
/// </summary>
public class ProgramParticipation : Entity, IAuditable, ISoftDelete
{
    public Guid StartupId { get; set; }
    public Startup Startup { get; set; } = null!;

    public Guid ProgramTermId { get; set; }
    public ProgramTerm ProgramTerm { get; set; } = null!;

    public ParticipationStatus Status { get; set; } = ParticipationStatus.Applied;
    public DateOnly JoinedOn { get; set; }
    public DateOnly? LeftOn { get; set; }
    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>Girişim ya da dönem pasife alındığında birlikte işaretlenir.</summary>
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
