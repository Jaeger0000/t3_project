using T3.Domain.Common;
using T3.Domain.Programs;

namespace T3.Domain.Identity;

/// <summary>
/// Program Yöneticisi'nin hangi programlardan sorumlu olduğunu tutar.
/// Yetki kapsamı filtresi (IStartupScope) bu tablo üzerinden çalışır.
/// </summary>
public class UserProgramAssignment : Entity, ISoftDelete
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid ProgramId { get; set; }
    public EcosystemProgram Program { get; set; } = null!;

    public DateTimeOffset AssignedAt { get; set; }

    /// <summary>Kullanıcı ya da program pasife alındığında birlikte işaretlenir.</summary>
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
