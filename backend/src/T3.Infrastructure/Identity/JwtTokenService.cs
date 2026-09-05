using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using T3.Application.Common.Interfaces;
using T3.Domain.Identity;

namespace T3.Infrastructure.Identity;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public AccessToken CreateAccessToken(User user, IReadOnlyCollection<Guid> assignedProgramIds)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(AppClaims.UserId, user.Id.ToString()),
            new(AppClaims.Role, user.Role.ToString()),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(AppClaims.SecurityStamp, user.SecurityStamp.ToString())
        };

        if (user.StartupId is { } startupId)
            claims.Add(new Claim(AppClaims.StartupId, startupId.ToString()));

        if (assignedProgramIds.Count > 0)
            claims.Add(new Claim(AppClaims.ProgramIds, string.Join(',', assignedProgramIds)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
