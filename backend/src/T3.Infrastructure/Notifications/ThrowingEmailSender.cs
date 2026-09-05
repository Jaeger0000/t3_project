using Microsoft.Extensions.Logging;
using T3.Application.Common.Interfaces;

namespace T3.Infrastructure.Notifications;

/// <summary>
/// Üretimde gerçek SMTP tanımlı değilken kayıtlı olan gönderici. Sessizce
/// diske yazmak yerine açıkça hata verir: üretimde şifre sıfırlama jetonunun
/// sunucu diskinde ikinci bir düz metin kopyası olarak durmasını önler
/// (bkz. G-02, Guvenlik_Denetimi_ve_Iyilestirme_Plani.md). Açılışta bir kez
/// uyarı log'u düşer ki yapılandırmayı okuyan kişi SMTP eksikliğini görsün.
/// </summary>
public sealed class ThrowingEmailSender(ILogger<ThrowingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        logger.LogError(
            "E-posta gönderilemedi: üretimde gerçek SMTP yapılandırılmadı (IEmailSender: ThrowingEmailSender). Konu: {Subject}",
            subject);
        throw new InvalidOperationException(
            "Üretimde gerçek bir SMTP sağlayıcısı yapılandırılmadı. " +
            "FileOutboxEmailSender üretimde kasıtlı olarak kayıtlı değil " +
            "(ham şifre sıfırlama jetonunu diske yazardı).");
    }
}
