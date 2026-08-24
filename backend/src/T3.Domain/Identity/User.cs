using T3.Domain.Common;
using T3.Domain.Startups;

namespace T3.Domain.Identity;

public class User : Entity, IAuditable, ISoftDelete
{
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public UserRole Role { get; set; }

    /// <summary>
    /// Yalnızca <see cref="UserRole.StartupUser"/> için dolu; kullanıcıyı
    /// yönetebileceği tek girişime bağlar.
    /// </summary>
    public Guid? StartupId { get; set; }
    public Startup? Startup { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>
    /// Yönetici şifre atadığında <c>true</c> olur, kullanıcı kendi şifresini
    /// belirlediğinde <c>false</c>. Amaç yöneticinin bildiği şifrenin kalıcı
    /// olmaması: aksi hâlde her hesabın şifresini bir başkası da biliyor.
    /// </summary>
    public bool MustChangePassword { get; set; }

    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = [];

    /// <summary>Program Yöneticisi'nin yetki kapsamını belirler.</summary>
    public ICollection<UserProgramAssignment> ProgramAssignments { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
