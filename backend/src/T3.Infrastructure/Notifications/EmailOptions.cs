namespace T3.Infrastructure.Notifications;

public class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Gönderen adresi. SMTP sağlayıcısında **doğrulanmış** olmalı.</summary>
    public string From { get; set; } = "no-reply@t3ekosistem.test";

    /// <summary>Gönderen görünen adı; alıcının kutusunda adresin yerine bu okunur.</summary>
    public string FromName { get; set; } = "T3 Girişim Ekosistemi";

    /// <summary>
    /// Yanıt adresi. Gönderen <c>no-reply@</c> olduğu için cevaplar boşluğa
    /// düşerdi; okunan bir kutu (ör. <c>info@</c>) buraya yazılırsa alıcı
    /// "yanıtla" dediğinde oraya gider. Boş bırakılırsa başlık eklenmez.
    /// </summary>
    public string? ReplyTo { get; set; }

    /// <summary>
    /// Bağlantıların işaret ettiği arayüz adresi. Sıfırlama bağlantısı
    /// e-postada gittiği için sunucu istekten değil yapılandırmadan okumak
    /// zorunda: <c>Host</c> başlığına güvenmek bağlantıyı saldırganın
    /// alan adına çevirebilirdi.
    /// </summary>
    public string AppBaseUrl { get; set; } = "http://localhost:5173";

    /// <summary>Geliştirme kutusu — gönderilen e-postalar buraya dosya olarak düşer.</summary>
    public string OutboxPath { get; set; } = "storage/outbox";

    public SmtpOptions Smtp { get; set; } = new();
}

/// <summary>
/// SMTP relay ayarları. Değerler ortam değişkeniyle gelir
/// (<c>T3_Email__Smtp__Host</c> → <c>Email:Smtp:Host</c>); parola hiçbir
/// koşulda <c>appsettings.json</c> içine yazılmaz.
/// </summary>
public sealed class SmtpOptions
{
    public string Host { get; set; } = "";

    /// <summary>587 = gönderim portu (STARTTLS). 465 kullanılacaksa <see cref="UseStartTls"/> kapatılır.</summary>
    public int Port { get; set; } = 587;

    /// <summary>Sağlayıcının verdiği giriş adı (Brevo'da hesabın e-posta adresi).</summary>
    public string User { get; set; } = "";

    /// <summary>Sağlayıcının SMTP anahtarı — hesap şifresi değil.</summary>
    public string Password { get; set; } = "";

    /// <summary>
    /// 587'de bağlantı düz başlar ve <c>STARTTLS</c> ile şifrelenir; 465'te
    /// baştan TLS kurulur. Yanlış seçim "bağlandı ama el sıkışmıyor" gibi
    /// görünen bir zaman aşımı üretir, o yüzden ayar açık tutuluyor.
    /// </summary>
    public bool UseStartTls { get; set; } = true;

    /// <summary>
    /// Sağlayıcı yanıt vermezse istek burada kesilir. Şifre sıfırlama ucu
    /// kullanıcıyı bekletiyor; sınırsız beklemek ucu kilitler.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 20;

    /// <summary>
    /// Üçü birden dolu değilse SMTP "yapılandırılmamış" sayılır ve gönderici
    /// seçimi ortama göre yapılır (bkz. DependencyInjection). Yarım
    /// yapılandırma sessizce kabul edilmiyor: eksik parolayla açılan bir
    /// bağlantı ancak ilk sıfırlama isteğinde patlardı.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(User)
        && !string.IsNullOrWhiteSpace(Password);
}
