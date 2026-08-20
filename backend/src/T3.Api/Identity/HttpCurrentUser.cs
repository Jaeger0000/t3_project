using System.Security.Claims;
using T3.Application.Common.Interfaces;
using T3.Domain.Identity;
using T3.Infrastructure.Identity;

namespace T3.Api.Identity;

/// <summary>
/// ICurrentUser'ın HTTP uygulaması: kimliği JWT claim'lerinden okur.
/// MCP tool çağrıları için aynı arayüzün ayrı bir uygulaması kullanılacak,
/// böylece yetki mantığı iki yolda da aynı kalır.
/// </summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirst(AppClaims.UserId)?.Value, out var id) ? id : null;

    public UserRole? Role =>
        Enum.TryParse<UserRole>(Principal?.FindFirst(AppClaims.Role)?.Value, out var role)
            ? role
            : null;

    public Guid? StartupId =>
        Guid.TryParse(Principal?.FindFirst(AppClaims.StartupId)?.Value, out var id) ? id : null;

    public IReadOnlyCollection<Guid> AssignedProgramIds
    {
        get
        {
            var raw = Principal?.FindFirst(AppClaims.ProgramIds)?.Value;

            if (string.IsNullOrWhiteSpace(raw))
                return [];

            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => Guid.TryParse(part, out var id) ? id : Guid.Empty)
                .Where(id => id != Guid.Empty)
                .ToArray();
        }
    }
}
