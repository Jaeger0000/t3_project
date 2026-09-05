using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Application.Common.Text;
using T3.Domain.Identity;

namespace T3.Application.Features.Auth.ChangePassword;

/// <summary>
/// Oturum içi şifre değiştirme.
///
/// Mevcut şifre yeniden isteniyor: jeton çalınmış bir oturumda saldırganın
/// şifreyi değiştirip hesabı tamamen devralmasını engelleyen tek kontrol bu.
/// Yönetici atamasıyla gelen "zorunlu değiştirme" bayrağı da burada düşüyor.
/// </summary>
public sealed class ChangePasswordHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IPasswordHasher passwordHasher,
    IAuditWriter audit,
    IUserStateProvider userState,
    ITokenService tokenService)
{
    public async Task<Result<ChangePasswordResponse>> Handle(
        ChangePasswordRequest request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            return Error.Forbidden("Oturum bulunamadı.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null || !user.IsActive)
            return Error.Forbidden("Hesabınız aktif değil.");

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            await audit.WriteForActorAsync(
                user.Id, user.Role,
                "Auth.PasswordChangeFailed", nameof(User), user.Id,
                after: new { Reason = "mevcut şifre hatalı" },
                ct: ct);

            return Error.Validation("Mevcut şifreniz hatalı.");
        }

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.MustChangePassword = false;

        // Elindeki her jeton (bu isteği yapan istemcininki dâhil) geçersiz
        // olur; aşağıda hemen yenisi üretilip döndürülüyor (bkz. G-01).
        user.SecurityStamp = Guid.NewGuid();

        // Açık sıfırlama bağlantıları harcanıyor: şifresini bilen kullanıcı
        // değiştirdiğine göre postadaki bağlantının yaşaması gereksiz risk.
        var pending = await db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null)
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;

        foreach (var resetToken in pending)
            resetToken.UsedAt = now;

        // Şifre değişikliği ile izi tek SaveChanges'ta kalıcı olur (bkz. G-09).
        await audit.WriteForActorAsync(
            user.Id, user.Role,
            "Auth.PasswordChanged", nameof(User), user.Id,
            after: new { Email = MaskedEmail.Of(user.Email) },
            ct: ct, saveChanges: false);

        await db.SaveChangesAsync(ct);
        userState.Invalidate(user.Id);

        var freshToken = tokenService.CreateAccessToken(user, currentUser.AssignedProgramIds);

        return new ChangePasswordResponse(now, freshToken.Value, freshToken.ExpiresAt);
    }
}
