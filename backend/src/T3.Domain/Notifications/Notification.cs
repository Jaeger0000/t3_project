using T3.Domain.Common;
using T3.Domain.Identity;
using T3.Domain.Startups;

namespace T3.Domain.Notifications;

/// <summary>
/// Yöneticiden (SuperAdmin/ProgramManager) bir girişime gönderilen serbest
/// metinli bildirim (ör. "ciro belgeni güncelle"). <see cref="AuditLog"/> ile
/// aynı felsefe: kayıt değiştirilemez kabul edilir, yalnızca eklenir —
/// <see cref="ReadAt"/> ve <see cref="RecipientDeletedAt"/> dışında hiçbir
/// alan sonradan güncellenmez.
/// </summary>
public class Notification : Entity
{
    public Guid StartupId { get; set; }
    public Startup Startup { get; set; } = null!;

    /// <summary>Bildirimi alan girişim kullanıcısı hesabı.</summary>
    public Guid RecipientUserId { get; set; }
    public User Recipient { get; set; } = null!;

    public Guid SentByUserId { get; set; }
    public User SentBy { get; set; } = null!;

    /// <summary>
    /// Gönderenin gönderim anındaki rolü. SuperAdmin'in gözetim ekranında
    /// "doğrudan gönderdiklerim" ile "Program Yöneticilerinin gönderdikleri"
    /// ayrımı bunun üzerinden yapılır — kullanıcının rolü sonradan değişse
    /// bile bu kayıt gönderim anındaki gerçeği taşır.
    /// </summary>
    public UserRole SentByRole { get; set; }

    public string Message { get; set; } = null!;

    public DateTimeOffset SentAt { get; set; }

    /// <summary>Alıcı bildirimler ekranını açtığında dolar; menüdeki rozet
    /// bunun null olup olmamasına bakar.</summary>
    public DateTimeOffset? ReadAt { get; set; }

    /// <summary>E-posta gönderimi başarısız olsa bile uygulama içi bildirim
    /// kalıcı olmalı — bu bayrak yalnızca bilgi amaçlı, akışı bloklamaz.</summary>
    public bool EmailSent { get; set; }

    /// <summary>
    /// Alıcı bildirimi kendi gelen kutusundan sildiğinde dolar. Kayıt
    /// gerçekten silinmez — yalnızca alıcının kendi listesinden (bkz.
    /// ListMyNotificationsHandler) çıkar. SuperAdmin'in gözetim ekranı bu
    /// alana bakmaz: alıcının kendi kutusunu düzenlemesi, Program
    /// Yöneticisi'nin ne gönderdiğine dair denetim izini silmemeli.
    /// </summary>
    public DateTimeOffset? RecipientDeletedAt { get; set; }
}
