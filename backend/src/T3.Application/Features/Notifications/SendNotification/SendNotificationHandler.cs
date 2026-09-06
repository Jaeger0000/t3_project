using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Rbac;
using T3.Application.Common.Results;
using T3.Application.Features.Users;
using T3.Domain.Identity;
using T3.Domain.Notifications;

namespace T3.Application.Features.Notifications.SendNotification;

/// <summary>
/// Yönetici (SuperAdmin/ProgramManager) girişime serbest metinli bir
/// bildirim gönderir. Bildirim iki kanaldan gider: uygulama içi (bkz.
/// ListMyNotificationsHandler, rozet) ve e-posta (bkz.
/// NotificationEmailTemplate) — e-posta gönderimi başarısız olsa bile
/// uygulama içi kayıt kalıcı olur, demo ortamında SMTP her zaman
/// yapılandırılı olmayabilir.
///
/// Program Yöneticisi gönderdiğinde SuperAdmin'e de bir kopya gider —
/// kullanıcı isteği: iki türü ekranda ayır ama SuperAdmin hiçbirini
/// gözden kaçırmasın.
/// </summary>
public sealed class SendNotificationHandler(
    IAppDbContext db,
    IStartupScope startupScope,
    ICurrentUser currentUser,
    IEmailSender emailSender,
    IAppLinkBuilder linkBuilder,
    IAuditWriter audit)
{
    public async Task<Result<NotificationRow>> Handle(
        Guid startupId, SendNotificationRequest request, CancellationToken ct)
    {
        // Handler'daki tekrar kontrol bilinçli: uç noktadaki politika MCP
        // üzerinden doğrudan çağrıldığında atlanabilir.
        if (currentUser.Role is not (UserRole.SuperAdmin or UserRole.ProgramManager))
            return Error.Forbidden("Bildirim gönderme yetkiniz yok.");

        // Kapsam: ProgramManager yalnızca kendi programındaki girişime
        // gönderebilir; kapsam dışı girişim için 404 (403 varlığını sızdırır).
        var startup = await startupScope.Apply(db.Startups.AsNoTracking())
            .FirstOrDefaultAsync(s => s.Id == startupId, ct);
        if (startup is null)
            return Error.NotFound("Girişim bulunamadı.");

        var recipients = await db.Users.AsNoTracking()
            .Where(u => u.StartupId == startupId && u.Role == UserRole.StartupUser && u.IsActive)
            .ToListAsync(ct);

        if (recipients.Count == 0)
            return Error.Validation("Bu girişime bağlı aktif bir kullanıcı hesabı yok.");

        var senderName = await db.Users.AsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(ct) ?? "—";

        var senderRole = currentUser.Role!.Value;
        var senderRoleLabel = UserLabels.Role(senderRole);
        var sentAt = DateTimeOffset.UtcNow;
        var message = request.Message.Trim();

        var appUrl = linkBuilder.BuildAppUrl("/bildirimler");
        var subject = NotificationEmailTemplate.Subject(startup.Name);
        var textBody = NotificationEmailTemplate.PlainText(startup.Name, senderName, senderRoleLabel, message, appUrl);
        var htmlBody = NotificationEmailTemplate.Html(startup.Name, senderName, senderRoleLabel, message, appUrl);

        var created = new List<Notification>();

        foreach (var recipient in recipients)
        {
            var notification = new Notification
            {
                StartupId = startupId,
                RecipientUserId = recipient.Id,
                SentByUserId = currentUser.UserId!.Value,
                SentByRole = senderRole,
                Message = message,
                SentAt = sentAt,
            };

            db.Notifications.Add(notification);
            created.Add(notification);

            notification.EmailSent = await TrySendAsync(recipient.Email, subject, textBody, htmlBody, ct);
        }

        // Program Yöneticisi gönderdiyse SuperAdmin'lere de kopya — bildirimin
        // kendisi değil yalnızca e-posta kopyası; uygulama içi kayıt SuperAdmin
        // gözetim ekranında zaten SentByRole ile görünür oluyor.
        if (senderRole == UserRole.ProgramManager)
        {
            var superAdmins = await db.Users.AsNoTracking()
                .Where(u => u.Role == UserRole.SuperAdmin && u.IsActive)
                .Select(u => u.Email)
                .ToListAsync(ct);

            var ccSubject = NotificationEmailTemplate.CcSubject(subject);
            foreach (var adminEmail in superAdmins)
                await TrySendAsync(adminEmail, ccSubject, textBody, htmlBody, ct);
        }

        await db.SaveChangesAsync(ct);

        var first = created[0];
        await audit.WriteAsync(
            "Notification.Send", nameof(Notification), first.Id,
            after: new
            {
                StartupId = startupId,
                RecipientCount = recipients.Count,
                SentByRole = senderRole,
                EmailSent = created.Count(n => n.EmailSent)
            },
            ct: ct);

        return new NotificationRow(
            first.Id, startupId, startup.Name, senderName, senderRole, senderRoleLabel,
            message, sentAt, null, first.EmailSent, null);
    }

    private async Task<bool> TrySendAsync(
        string to, string subject, string textBody, string htmlBody, CancellationToken ct)
    {
        try
        {
            await emailSender.SendHtmlAsync(to, subject, textBody, htmlBody, ct);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // E-posta gönderilemese de uygulama içi bildirim kalıcı olur
            // (bkz. sınıf yorumu) — bu yüzden hata burada yutulur.
            return false;
        }
    }
}
