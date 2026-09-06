namespace T3.Application.Common.Interfaces;

/// <summary>
/// E-posta gönderimi. Creathon kapsamında gerçek SMTP kurulmadığı için tek
/// uygulaması geliştirme kutusuna (dosya + log) yazıyor; arayüz yine de burada
/// duruyor çünkü şifre sıfırlama akışı sağlayıcıya bağlı olmamalı — SMTP
/// geldiğinde yalnızca kayıt satırı değişir.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);

    /// <summary>
    /// HTML gövdeli e-posta (bildirim postası gibi markalı içerik için).
    /// Şifre sıfırlama bilinçli olarak düz metin kalıyor (bkz. SmtpEmailSender
    /// yorumu); yalnızca gerçekten HTML üretebilen gönderici (SMTP, dosya
    /// kutusu) bunu geçersiz kılar. Varsayılan uygulama düz metne düşer —
    /// yeni bir gönderici eklense bile derleme kırılmaz.
    /// </summary>
    Task SendHtmlAsync(
        string to, string subject, string textBody, string htmlBody, CancellationToken ct = default) =>
        SendAsync(to, subject, textBody, ct);
}
