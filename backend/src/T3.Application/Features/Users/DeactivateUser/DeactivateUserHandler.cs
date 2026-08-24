using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Domain.Identity;

namespace T3.Application.Features.Users.DeactivateUser;

/// <summary>
/// Hesabı erişimden düşürür. Kayıt silinmiyor: kullanıcı denetim izindeki
/// eylemlerin aktörü ve önerilerin göndericisi; satır giderse iz sahipsiz
/// kalır. Giriş akışı <c>IsActive</c> kontrol ettiği için erişim anında kesilir.
/// </summary>
public sealed class DeactivateUserHandler(
    IAppDbContext db,
    UserAdminGuard guard,
    IAuditWriter audit)
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

        // Program atamaları da kaldırılıyor: hesap yeniden açılırsa yetki
        // kapsamı bilinçli olarak tekrar verilsin, eski kapsam sessizce geri
        // dönmesin.
        foreach (var assignment in user.ProgramAssignments.Where(a => !a.IsDeleted))
            assignment.IsDeleted = true;

        await db.SaveChangesAsync(ct);

        await audit.WriteAsync(
            "User.Deactivate", nameof(User), user.Id,
            before: new { user.Email, IsActive = true },
            after: new { user.Email, user.IsActive },
            ct: ct);

        return true;
    }
}
