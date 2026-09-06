using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;

namespace T3.Application.Features.Notifications.MarkNotificationsRead;

/// <summary>
/// Oturum sahibinin tüm okunmamış bildirimlerini okunmuş işaretler —
/// "bildirimler ekranını açınca menüdeki rozet gitsin" isteğinin karşılığı.
/// Tekil değil toplu: ekran açılışında bir kerede temizleniyor, tek tek
/// işaretleme ekranı gerekmiyor (bkz. NotificationsPage.tsx).
///
/// <c>ExecuteUpdateAsync</c> kullanılmıyor — proje kuralı Application
/// katmanında sağlayıcıya özel EF API'sini yasaklıyor.
/// </summary>
public sealed class MarkNotificationsReadHandler(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<Result<int>> Handle(CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Error.Forbidden("Oturum açmalısınız.");

        var unread = await db.Notifications
            .Where(n => n.RecipientUserId == currentUser.UserId && n.ReadAt == null)
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        foreach (var notification in unread)
            notification.ReadAt = now;

        await db.SaveChangesAsync(ct);

        return unread.Count;
    }
}
