using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Application.Common.Text;
using T3.Domain.Identity;

namespace T3.Application.Features.Auth.Login;

public sealed class LoginHandler(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IAuditWriter audit)
{
    /// <summary>
    /// Kimlik doğrulama hatalarında e-postanın kayıtlı olup olmadığı bilgisi
    /// sızdırılmaz: yanlış şifre, olmayan kullanıcı ve pasif hesap aynı mesajı
    /// döner. Böylece giriş ekranı kullanıcı adı numaralandırmaya hizmet etmez.
    ///
    /// Ayrım yalnızca denetim izine yazılıyor: "hangi hesap kilitleniyor" ve
    /// "var olmayan adresler mi taranıyor" güvenlik incelemesinin iki farklı
    /// sorusu ve ize yalnızca sistem yöneticisi erişiyor.
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

        if (user is null)
            return await FailAsync(request.Email, "kayıtlı olmayan adres", null, ct);

        if (!user.IsActive)
            return await FailAsync(request.Email, "hesap pasif", user, ct);

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            return await FailAsync(request.Email, "şifre hatalı", user, ct);

        var programs = user.ProgramAssignments
            .Select(a => new AssignedProgramResponse(a.ProgramId, a.Program.Name))
            .OrderBy(p => p.Name)
            .ToArray();

        var token = tokenService.CreateAccessToken(
            user, programs.Select(p => p.Id).ToArray());

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        // Aktör açıkça veriliyor: jeton bu isteğin içinde üretildi, henüz hiçbir
        // ardışık düzen bileşeni kullanıcıyı tanımıyor.
        await audit.WriteForActorAsync(
            user.Id, user.Role,
            "Auth.LoginSucceeded", nameof(User), user.Id,
            after: new { Email = MaskedEmail.Of(user.Email), Role = user.Role.ToString() },
            ct: ct);

        return new LoginResponse(
            token.Value,
            token.ExpiresAt,
            SessionUserMapper.Map(user, programs));
    }

    /// <summary>
    /// Başarısız denemeyi ize yazar ve her durumda aynı hatayı döner.
    /// E-posta maskelenerek saklanıyor — iz, denenen adreslerin ham listesine
    /// dönüşmemeli.
    /// </summary>
    private async Task<Result<LoginResponse>> FailAsync(
        string attemptedEmail, string reason, User? user, CancellationToken ct)
    {
        await audit.WriteForActorAsync(
            user?.Id, user?.Role,
            "Auth.LoginFailed", nameof(User), user?.Id,
            after: new { Email = MaskedEmail.Of(attemptedEmail), Reason = reason },
            ct: ct);

        return InvalidCredentials;
    }
}
