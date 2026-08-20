using T3.Domain.Identity;

namespace T3.Application.Common.Interfaces;

/// <summary>
/// İsteği yapan kullanıcının kimliği. HTTP bağlamından da (REST) MCP tool
/// çağrısından da aynı arayüz doldurulur — böylece yetki mantığı iki yolda
/// aynı şekilde işler.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    UserRole? Role { get; }

    /// <summary>StartupUser rolünde kullanıcının bağlı olduğu girişim.</summary>
    Guid? StartupId { get; }

    /// <summary>ProgramManager rolünde kullanıcının sorumlu olduğu program kimlikleri.</summary>
    IReadOnlyCollection<Guid> AssignedProgramIds { get; }

    bool IsAuthenticated { get; }
}
