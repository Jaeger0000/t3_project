using T3.Domain.Common;
using T3.Domain.Identity;

namespace T3.Domain.Programs;

/// <summary>
/// T3 girişimcilik programı (Take Off, Ön Kuluçka, TEKNOFEST, DENEYAP …).
/// Adı bilinçli olarak <c>Program</c> değil: ASP.NET Core'un giriş noktası
/// olan <c>Program</c> sınıfıyla isim çakışmasını önlüyor.
/// </summary>
public class EcosystemProgram : Entity, IAuditable, ISoftDelete
{
    public string Name { get; set; } = null!;
    public ProgramType Type { get; set; }

    /// <summary>Programı yürüten koordinatörlük (Girişim Merkezi, TEKNOFEST …).</summary>
    public string? Coordinatorship { get; set; }

    public string? Description { get; set; }

    public ICollection<ProgramTerm> Terms { get; set; } = [];
    public ICollection<UserProgramAssignment> ManagerAssignments { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
