using T3.Domain.Identity;

namespace T3.Application.Common.Interfaces;

public record AccessToken(string Value, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    AccessToken CreateAccessToken(User user, IReadOnlyCollection<Guid> assignedProgramIds);
}
