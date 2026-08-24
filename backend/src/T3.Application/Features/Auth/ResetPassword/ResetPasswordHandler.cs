using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Application.Common.Text;
using T3.Domain.Identity;

namespace T3.Application.Features.Auth.ResetPassword;

/// <summary>
/// Sıfırlama bağlantısını harcayıp yeni şifreyi yazar.
///
/// Üç kontrol de aynı hatayı döner: jeton bilinmiyor, süresi geçmiş ya da
/// kullanılmış. Ayrımı söylemek, elinde jeton olan birine "bu jeton gerçekti
/// ama süresi doldu" bilgisini vermek olurdu; kullanıcı için üçü de tek eylem
/// gerektiriyor — yeniden bağlantı istemek.
/// </summary>
public sealed class ResetPasswordHandler(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    IAuditWriter audit)
{
    private static readonly Error InvalidToken = Error.Validation(
        "Sıfırlama bağlantısı geçersiz ya da süresi dolmuş. Yeni bir bağlantı isteyin.");

    public async Task<Result<ResetPasswordResponse>> Handle(
        ResetPasswordRequest request, CancellationToken ct)
    {
        var hash = PasswordResetSecrets.Hash(request.Token);

        var token = await db.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (token is null || token.UsedAt is not null || token.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            await audit.WriteForActorAsync(
                actorUserId: null, actorRole: null,
                "Auth.PasswordResetFailed", nameof(User), token?.UserId,
                after: new
                {
                    Reason = token is null
                        ? "bilinmeyen jeton"
                        : token.UsedAt is not null ? "jeton kullanılmış" : "jeton süresi dolmuş"
                },
                ct: ct);

            return InvalidToken;
        }

        if (!token.User.IsActive)
            return InvalidToken;

        var now = DateTimeOffset.UtcNow;

        token.UsedAt = now;
        token.User.PasswordHash = passwordHasher.Hash(request.NewPassword);

        // Kullanıcı şifresini kendisi belirledi: zorunlu değiştirme bayrağı düşer.
        token.User.MustChangePassword = false;

        await db.SaveChangesAsync(ct);

        await audit.WriteForActorAsync(
            token.UserId, token.User.Role,
            "Auth.PasswordReset", nameof(User), token.UserId,
            after: new { Email = MaskedEmail.Of(token.User.Email) },
            ct: ct);

        // Yanıtta ham e-posta yok: bağlantıyı ele geçiren biri hangi hesaba
        // ait olduğunu buradan öğrenmesin.
        return new ResetPasswordResponse(MaskedEmail.Of(token.User.Email), now);
    }
}
