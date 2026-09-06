using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Application.Features.Users;
using T3.Domain.Identity;

namespace T3.Application.Features.Notifications.ListSentNotifications;

/// <summary>
/// SuperAdmin'in gözetim ekranı: sistemdeki TÜM bildirimler, gönderenin rolüne
/// göre ikiye ayrılmış döner — kullanıcı isteği tam olarak buydu ("direkt
/// girişimler ve program yöneticilerinin attığı bildirimleri ayır ki
/// karıştırmasın"). Kapsam daraltması yok: SuperAdmin zaten tüm ekosistemi
/// görüyor, satır düzeyi bir IStartupScope kısıtı burada anlamsız.
/// </summary>
public sealed record SentNotificationsResponse(
    IReadOnlyList<NotificationRow> DirectFromSuperAdmin,
    IReadOnlyList<NotificationRow> FromProgramManagers);

public sealed class ListSentNotificationsHandler(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<Result<SentNotificationsResponse>> Handle(CancellationToken ct)
    {
        if (currentUser.Role is not UserRole.SuperAdmin)
            return Error.Forbidden("Bu ekranı görme yetkiniz yok.");

        var rows = await db.Notifications.AsNoTracking()
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

        return new SentNotificationsResponse(
            items.Where(i => i.SentByRole == UserRole.SuperAdmin).ToList(),
            items.Where(i => i.SentByRole == UserRole.ProgramManager).ToList());
    }
}
