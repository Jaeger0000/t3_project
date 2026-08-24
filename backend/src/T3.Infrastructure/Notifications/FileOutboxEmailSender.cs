using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using T3.Application.Common.Interfaces;

namespace T3.Infrastructure.Notifications;

/// <summary>
/// Geliştirme/demo gönderici: e-postayı diske yazar ve log'a düşürür.
///
/// Sessizce yutan bir "boş uygulama" bilinçli olarak seçilmedi — şifre
/// sıfırlama akışının uçtan uca doğrulanabilir olması gerekiyor ve HTTP yanıtına
/// jeton koymak (kolay yol) sıfırlamayı isteyen herkese hesabı devretmek
/// olurdu. Kutu dosyası sunucunun diskinde durur, ağdan erişilemez.
/// </summary>
public sealed class FileOutboxEmailSender(
    IOptions<EmailOptions> options,
    ILogger<FileOutboxEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(
        string to, string subject, string body, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_options.OutboxPath);

        // Dosya adı zamana göre sıralanabilir olmalı: doğrulama betiği "en son
        // gönderilen" e-postayı okuyor.
        var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
        var safeRecipient = string.Concat(to.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
        var path = Path.Combine(_options.OutboxPath, $"{stamp}-{safeRecipient}.txt");

        var content = $"""
            Kime: {to}
            Kimden: {_options.From}
            Konu: {subject}
            Tarih: {DateTimeOffset.UtcNow:O}

            {body}
            """;

        await File.WriteAllTextAsync(path, content, ct);

        logger.LogInformation(
            "E-posta gönderilmedi, geliştirme kutusuna yazıldı: {Path} (konu: {Subject})",
            path, subject);
    }
}
