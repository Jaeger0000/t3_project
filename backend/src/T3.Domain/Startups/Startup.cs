using T3.Domain.Achievements;
using T3.Domain.Common;
using T3.Domain.Documents;
using T3.Domain.Milestones;
using T3.Domain.Programs;

namespace T3.Domain.Startups;

/// <summary>
/// Merkezi girişim kartının gövdesi (MVP #1). Bir girişimin T3 ekosistemindeki
/// tek doğrulanmış kaydı burasıdır; program geçmişi, finansallar ve dokümanlar
/// bu köke bağlanır.
/// </summary>
public class Startup : Entity, IAuditable, ISoftDelete
{
    public string Name { get; set; } = null!;
    public string? LegalName { get; set; }

    /// <summary>Hassas veri — yalnızca SuperAdmin ve girişimin kendisi görür.</summary>
    public string? TaxNumber { get; set; }

    public DateOnly? FoundedOn { get; set; }
    public Sector Sector { get; set; }

    /// <summary>Postgres text[] kolonu olarak saklanır.</summary>
    public List<string> TechnologyAreas { get; set; } = [];

    public string? ProductDescription { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public string? City { get; set; }

    /// <summary>Hassas veri (KVKK) — Karar Verici rolüne maskelenir.</summary>
    public string? ContactEmail { get; set; }

    /// <summary>Hassas veri (KVKK) — Karar Verici rolüne maskelenir.</summary>
    public string? ContactPhone { get; set; }

    public StartupStatus Status { get; set; } = StartupStatus.Active;

    public ICollection<TeamMember> TeamMembers { get; set; } = [];
    public ICollection<ProgramParticipation> Participations { get; set; } = [];
    public ICollection<Achievement> Achievements { get; set; } = [];
    public ICollection<Document> Documents { get; set; } = [];
    public ICollection<Milestone> Milestones { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
