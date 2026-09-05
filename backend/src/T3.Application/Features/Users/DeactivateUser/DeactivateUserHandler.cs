using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Domain.Identity;

namespace T3.Application.Features.Users.DeactivateUser;

/// <summary>
/// Hesabı erişimden düşürür. Kayıt silinmiyor: kullanıcı denetim izindeki
/// eylemlerin aktörü ve önerilerin göndericisi; satır giderse iz sahipsiz
/// kalır. Erişim yalnızca yeni girişte değil — <c>SecurityStamp</c>
/// yenilendiği için <c>UserStateMiddleware</c> üzerinden elindeki jetonu olan
/// kullanıcının da bir sonraki isteğinde kesilir (bkz. G-01).
/// </summary>
public sealed class DeactivateUserHandler(
    IAppDbContext db,
    UserAdminGuard guard,
    IAuditWriter audit,
    IUserStateProvider userState)
{
    public async Task<Result<bool>> Handle(Guid id, CancellationToken ct)
    {
        if (guard.EnsureCanManage() is { } denied)
            return denied;

        if (guard.EnsureNotSelf(id, "hesap kapatma") is { } self)
            return self;

        var user = await db.Users
            .Include(u => u.ProgramAssignments)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user is null)
            return Error.NotFound("Kullanıcı bulunamadı.");

        if (!user.IsActive)
            return Error.Conflict("Bu hesap zaten pasif.");

        user.IsActive = false;
        user.SecurityStamp = Guid.NewGuid();

        // Program atamaları da kaldırılıyor: hesap yeniden açılırsa yetki
        // kapsamı bilinçli olarak tekrar verilsin, eski kapsam sessizce geri
        // dönmesin.
        foreach (var assignment in user.ProgramAssignments.Where(a => !a.IsDeleted))
            assignment.IsDeleted = true;

        // Hesap değişikliği ile denetim izi tek SaveChanges'ta birlikte kalıcı
        // olur — ikisi ayrı çağrılarda olsaydı süreç arada düşerse veri
        // değişir ama izi kaybolurdu (bkz. G-09).
        await audit.WriteAsync(
            "User.Deactivate", nameof(User), user.Id,
            before: new { user.Email, IsActive = true },
            after: new { user.Email, user.IsActive },
            ct: ct, saveChanges: false);

        await db.SaveChangesAsync(ct);
        userState.Invalidate(user.Id);

        return true;
    }
}
