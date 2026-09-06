using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Application.Features.Users;

namespace T3.Application.Features.Notifications.ListMyNotifications;

public sealed record MyNotificationsResponse(
    IReadOnlyList<NotificationRow> Items, int UnreadCount);

/// <summary>
/// Oturum sahibinin kendi bildirim gelen kutusu. Sahiplik satırın kendisinde
/// (<c>RecipientUserId == currentUser.UserId</c>) — ayrı bir kapsam nesnesi
/// gerekmiyor, çünkü kural "yalnızca kendisi" kadar basit ve tek yerde.
///
/// Silinen bildirimler artık listeden hiç çıkarılmıyor — "Silinenler"
/// sekmesinin (bkz. NotificationsPage.tsx) bir şey gösterebilmesi için
/// `DeletedByRecipientAt` dolu satırlar da dönüyor, yalnızca istemci
/// tarafında filtreleniyor.
/// </summary>
public sealed class ListMyNotificationsHandler(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<Result<MyNotificationsResponse>> Handle(CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Error.Forbidden("Oturum açmalısınız.");

        var rows = await db.Notifications.AsNoTracking()
            .Where(n => n.RecipientUserId == currentUser.UserId)
            .OrderByDescending(n => n.SentAt)
            .Select(n => new
            {
                n.Id,
                n.StartupId,
                StartupName = n.Startup.Name,
                SentByName = n.SentBy.FullName,
                n.SentByRole,
                n.Message,
                n.SentAt,
                n.ReadAt,
                n.EmailSent,
                n.RecipientDeletedAt
            })
            .ToListAsync(ct);

        var items = rows
            .Select(r => new NotificationRow(
                r.Id, r.StartupId, r.StartupName, r.SentByName, r.SentByRole,
                UserLabels.Role(r.SentByRole), r.Message, r.SentAt, r.ReadAt, r.EmailSent,
                r.RecipientDeletedAt))
            .ToList();

        // Silinmiş bir bildirim okunmamış olsa bile rozeti şişirmemeli —
        // kullanıcı zaten onu kendi tarafında kapatmış sayılır.
        var unreadCount = items.Count(i => i.ReadAt is null && i.DeletedByRecipientAt is null);

        return new MyNotificationsResponse(items, unreadCount);
    }
}
