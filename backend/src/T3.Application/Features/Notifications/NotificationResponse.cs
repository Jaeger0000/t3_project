using T3.Domain.Identity;

namespace T3.Application.Features.Notifications;

public sealed record NotificationRow(
    Guid Id,
    Guid StartupId,
    string StartupName,
    string SentByName,
    UserRole SentByRole,
    string SentByRoleLabel,
    string Message,
    DateTimeOffset SentAt,
    DateTimeOffset? ReadAt,
    bool EmailSent,
    /// <summary>
    /// Alıcı bu bildirimi kendi kutusundan sildiğinde dolar (bkz.
    /// DeleteNotificationHandler). Kendi gelen kutumda "Silinenler" sekmesi,
    /// SuperAdmin'in gözetim ekranında ise "hangi girişim hangi bildirimi
    /// kendi tarafında sildi" sinyali bunun üzerinden okunuyor.
    /// </summary>
    DateTimeOffset? DeletedByRecipientAt);
