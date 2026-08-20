using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Application.Common.Text;

namespace T3.Application.Features.Auth.Login;

public sealed class LoginHandler(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService)
{
    /// <summary>
    /// Kimlik doğrulama hatalarında e-postanın kayıtlı olup olmadığı bilgisi
    /// sızdırılmaz: yanlış şifre, olmayan kullanıcı ve pasif hesap aynı mesajı
    /// döner. Böylece giriş ekranı kullanıcı adı numaralandırmaya hizmet etmez.
    /// </summary>
    private static readonly Error InvalidCredentials =
        Error.Validation("E-posta veya şifre hatalı.");

    public async Task<Result<LoginResponse>> Handle(LoginRequest request, CancellationToken ct)
    {
        var email = SearchText.Normalize(request.Email);

        var user = await db.Users
            .Include(u => u.Startup)
            .Include(u => u.ProgramAssignments)
                .ThenInclude(a => a.Program)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email, ct);

        if (user is null || !user.IsActive)
            return InvalidCredentials;

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            return InvalidCredentials;

        var programs = user.ProgramAssignments
            .Select(a => new AssignedProgramResponse(a.ProgramId, a.Program.Name))
            .OrderBy(p => p.Name)
            .ToArray();

        var token = tokenService.CreateAccessToken(
            user, programs.Select(p => p.Id).ToArray());

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return new LoginResponse(
            token.Value,
            token.ExpiresAt,
            SessionUserMapper.Map(user, programs));
    }
}
