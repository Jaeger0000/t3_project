using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;

namespace T3.Application.Features.Notifications.RestoreNotification;

/// <summary>
/// Alıcı, kendi sildiği bir bildirimi "Silinenler" sekmesinden geri
/// yükler — <c>RecipientDeletedAt</c>'i temizler. SuperAdmin'in gözetim
/// ekranındaki "Silinenler" görünümü salt okunur (bkz. NotificationsPage.tsx);
/// bu yüzden burada SuperAdmin bypass'ı yok, yalnızca gerçek alıcı geri
/// yükleyebilir.
/// </summary>
public sealed class RestoreNotificationHandler(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<Result<bool>> Handle(Guid notificationId, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Error.Forbidden("Oturum açmalısınız.");

        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientUserId == currentUser.UserId, ct);

        if (notification is null)
            return Error.NotFound("Bildirim bulunamadı.");

        notification.RecipientDeletedAt = null;
        await db.SaveChangesAsync(ct);

        return true;
    }
}
