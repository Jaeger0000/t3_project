using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Text;

namespace T3.Infrastructure.Notifications;

/// <summary>
/// Gerçek gönderici: e-postayı bir SMTP relay'i (üretimde Brevo) üzerinden
/// yollar.
///
/// Neden kendi mail sunucumuz değil: 25. port çoğu VPS'te kapalı, yeni bir IP'nin
/// itibarı yokken şifre sıfırlama maili doğrudan spam'e düşer. Relay, alan adı
/// SPF/DKIM ile yetkilendirildiği sürece bu işi bizim bakımımız olmadan yapıyor
/// (kurulum: docs/Mail_Servisi_Kurulumu.md).
///
/// Neden <c>System.Net.Mail.SmtpClient</c> değil: Microsoft yeni kod için
/// önermiyor; MailKit STARTTLS ve iptal jetonunu doğru işliyor.
/// </summary>
public sealed class SmtpEmailSender(
    IOptions<EmailOptions> options,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public Task SendAsync(
        string to, string subject, string body, CancellationToken ct = default) =>
        SendMessageAsync(to, subject, BuildMessage(_options, to, subject, body), ct);

    public Task SendHtmlAsync(
        string to, string subject, string textBody, string htmlBody, CancellationToken ct = default) =>
        SendMessageAsync(to, subject, BuildHtmlMessage(_options, to, subject, textBody, htmlBody), ct);

    /// <summary>
    /// Bağlan/gönder/kapat döngüsü düz metin ve HTML gönderiminde birebir
    /// aynı — tek fark mesaj gövdesinin nasıl kurulduğu (bkz. BuildMessage /
    /// BuildHtmlMessage), o yüzden burada bir kez yazılıyor.
    /// </summary>
    private async Task SendMessageAsync(
        string to, string subject, MimeMessage message, CancellationToken ct)
    {
        var smtp = _options.Smtp;

        using var client = new SmtpClient
        {
            Timeout = smtp.TimeoutSeconds * 1000
        };

        try
        {
            // Otomatik seçim yerine açık seçim: sağlayıcı sertifikayı gecikmeli
            // sunduğunda "auto" bazen düz metne düşüyor ve parola ağda açık
            // gidiyor. Yanlış portta hata almak, sessizce şifresiz göndermekten
            // iyidir.
            var security = smtp.UseStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.SslOnConnect;

            await client.ConnectAsync(smtp.Host, smtp.Port, security, ct);
            await client.AuthenticateAsync(smtp.User, smtp.Password, ct);
            await client.SendAsync(message, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Alıcı **maskeli** yazılıyor (iz kaydıyla aynı kural, bkz. G-05):
            // log satırı denenen adreslerin ham listesine dönüşmemeli.
            logger.LogError(
                ex, "E-posta gönderilemedi: {Recipient} (konu: {Subject}, sunucu: {Host}:{Port})",
                MaskedEmail.Of(to), subject, smtp.Host, smtp.Port);
            throw;
        }
        finally
        {
            if (client.IsConnected)
                // İptal edilmiş jetonla kapatmaya çalışmak bağlantıyı yarım
                // bırakır; kapanış her hâlükârda tamamlanmalı.
                await client.DisconnectAsync(quit: true, CancellationToken.None);
        }

        // Gövde bilerek loglanmıyor: şifre sıfırlama bağlantısı ham jeton
        // taşıyor ve log'a düşerse jetonun ikinci bir kopyası oluşurdu.
        logger.LogInformation(
            "E-posta gönderildi: {Recipient} (konu: {Subject})", MaskedEmail.Of(to), subject);
    }

    /// <summary>
    /// Mesaj kurulumu gönderimden ayrı duruyor ki başlıkların doğruluğu
    /// (gönderen, yanıt adresi, düz metin gövde) SMTP sunucusuna ihtiyaç
    /// duymadan test edilebilsin.
    /// </summary>
    public static MimeMessage BuildMessage(
        EmailOptions options, string to, string subject, string body)
    {
        var message = NewMessage(options, to, subject);

        // Düz metin: sıfırlama e-postası tek bağlantıdan ibaret, HTML gövde
        // hem spam puanını yükseltir hem de bağlantıyı gizleyebileceği için
        // kullanıcıya adresi olduğu gibi göstermenin önüne geçerdi.
        message.Body = new TextPart("plain") { Text = body };

        return message;
    }

    /// <summary>
    /// HTML + düz metin ikilisi (multipart/alternative): HTML render etmeyen
    /// istemci (ya da ekran okuyucu) düz metne düşer, bu yüzden ikisi de
    /// birlikte gidiyor — yalnızca HTML yollamak erişilebilirliği kırardı.
    /// </summary>
    public static MimeMessage BuildHtmlMessage(
        EmailOptions options, string to, string subject, string textBody, string htmlBody)
    {
        var message = NewMessage(options, to, subject);
        message.Body = new BodyBuilder { TextBody = textBody, HtmlBody = htmlBody }.ToMessageBody();
        return message;
    }

    private static MimeMessage NewMessage(EmailOptions options, string to, string subject)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(options.FromName, options.From));
        message.To.Add(MailboxAddress.Parse(to));

        if (!string.IsNullOrWhiteSpace(options.ReplyTo))
            message.ReplyTo.Add(MailboxAddress.Parse(options.ReplyTo));

        message.Subject = subject;
        return message;
    }
}
