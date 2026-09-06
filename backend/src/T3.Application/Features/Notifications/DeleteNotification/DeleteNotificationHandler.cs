using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Domain.Identity;

namespace T3.Application.Features.Notifications.DeleteNotification;

/// <summary>
/// Bildirim silme iki farklı anlama geliyor, kim sildiğine göre:
///
/// - Alıcı kendi gelen kutusundan siliyorsa kayıt gerçekten silinmez —
///   yalnızca <c>RecipientDeletedAt</c> dolar ve alıcının kendi listesinden
///   çıkar (bkz. ListMyNotificationsHandler). SuperAdmin'in gözetim ekranı bu
///   alana bakmaz: alıcının kendi kutusunu düzenlemesi, Program
///   Yöneticisi'nin ne gönderdiğine dair denetim izini silmemeli.
/// - SuperAdmin kendi gözetim ekranından siliyorsa (alıcı DEĞİLKEN) kayıt
///   gerçekten kaldırılır — SuperAdmin'in "sil" demesi hem oradan hem
///   alıcının kendi kutusundan gitmesi anlamına geliyor; yarı silinmiş bir
///   kayıt SuperAdmin'in gözetim ekranında görünmeye devam edip alıcıda
///   kaybolsaydı (ya da tersi) kafa karıştırırdı.
/// </summary>
public sealed class DeleteNotificationHandler(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<Result<bool>> Handle(Guid notificationId, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Error.Forbidden("Oturum açmalısınız.");

        var isSuperAdmin = currentUser.Role == UserRole.SuperAdmin;

        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId
                && (n.RecipientUserId == currentUser.UserId || isSuperAdmin), ct);

        if (notification is null)
            return Error.NotFound("Bildirim bulunamadı.");

        if (isSuperAdmin)
            db.Notifications.Remove(notification);
        else
            notification.RecipientDeletedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        return true;
    }
}
