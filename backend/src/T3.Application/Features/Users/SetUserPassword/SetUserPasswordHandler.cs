using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Domain.Identity;

namespace T3.Application.Features.Users.SetUserPassword;

public sealed record SetUserPasswordResponse(Guid Id, string Email, DateTimeOffset ChangedAt);

public sealed class SetUserPasswordHandler(
    IAppDbContext db,
    UserAdminGuard guard,
    IPasswordHasher passwordHasher,
    IAuditWriter audit)
{
    public async Task<Result<SetUserPasswordResponse>> Handle(
        Guid id, SetUserPasswordRequest request, CancellationToken ct)
    {
        if (guard.EnsureCanManage() is { } denied)
            return denied;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user is null)
            return Error.NotFound("Kullanıcı bulunamadı.");

        user.PasswordHash = passwordHasher.Hash(request.Password);
        await db.SaveChangesAsync(ct);

        // İzde yalnızca olayın kendisi var: ne şifre ne özeti yazılıyor.
        // Denetim izinin amacı "kim değiştirdi" sorusuna cevap vermek;
        // sırrın ikinci bir kopyasını üretmek değil.
        await audit.WriteAsync(
            "User.SetPassword", nameof(User), user.Id,
            after: new { user.Email },
            ct: ct);

        return new SetUserPasswordResponse(user.Id, user.Email, DateTimeOffset.UtcNow);
    }
}
