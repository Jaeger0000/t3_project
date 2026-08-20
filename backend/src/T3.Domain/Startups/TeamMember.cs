using T3.Domain.Common;

namespace T3.Domain.Startups;

/// <summary>
/// Girişim ekibi. Tüm alanları kişisel veri sayılır; Karar Verici rolüne
/// isim dışındaki alanlar maskelenerek döner.
/// </summary>
public class TeamMember : Entity, IAuditable, ISoftDelete
{
    public Guid StartupId { get; set; }
    public Startup Startup { get; set; } = null!;

    public string FullName { get; set; } = null!;
    public string? Title { get; set; }

    /// <summary>Hassas veri (KVKK).</summary>
    public string? Email { get; set; }

    /// <summary>Hassas veri (KVKK).</summary>
    public string? Phone { get; set; }

    public string? LinkedInUrl { get; set; }
    public bool IsFounder { get; set; }
    public DateOnly? JoinedOn { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
