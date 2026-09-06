using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Domain.Identity;

namespace T3.Application.Features.Notifications.MarkNotificationRead;

/// <summary>
/// Tek bir bildirimi okunmuş işaretler — bildirimler ekranındaki "Okundu
/// işaretle" düğmesi. Toplu işaretleme (<c>MarkNotificationsReadHandler</c>)
/// hâlâ duruyor, ikisi birbirini dışlamıyor.
///
/// SuperAdmin, kendi gözetim ekranından (<c>ListSentNotificationsHandler</c>)
/// gördüğü HERHANGİ bir bildirimi de okundu işaretleyebilir — oradaki
/// "Okundu işaretle" düğmesi aynı uca gidiyor; alıcı olmayan tek istisna bu.
/// </summary>
public sealed class MarkNotificationReadHandler(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<Result<bool>> Handle(Guid notificationId, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Error.Forbidden("Oturum açmalısınız.");

        var isSuperAdmin = currentUser.Role == UserRole.SuperAdmin;

        // Kapsam dışı (başkasının, SuperAdmin değilken) bildirim için 404:
        // 403 varlığını sızdırır.
        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId
                && (n.RecipientUserId == currentUser.UserId || isSuperAdmin), ct);

        if (notification is null)
            return Error.NotFound("Bildirim bulunamadı.");

        if (notification.ReadAt is null)
        {
            notification.ReadAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return true;
    }
}
