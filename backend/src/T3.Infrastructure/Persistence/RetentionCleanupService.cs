using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using T3.Domain.Audit;
using T3.Domain.Identity;

namespace T3.Infrastructure.Persistence;

/// <summary>
/// Süresiz büyüyen iki kişisel veri deposunu günlük olarak temizler:
/// <see cref="PasswordResetToken"/> (süresi dolanlar silinir) ve
/// <see cref="AuditLog"/> (10 yıldan eskiler kişisel veriden arındırılır,
/// olayın kendisi kalır). Aydınlatma metninde (PrivacyNoticePage) yazılan
/// saklama sürelerinin kod karşılığı burası — metin ile kod ayrışmasın diye
/// (bkz. G-09, Guvenlik_Denetimi_ve_Iyilestirme_Plani.md).
/// </summary>
public sealed class RetentionCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<RetentionCleanupService> logger) : BackgroundService
{
    /// <summary>Denetim izi saklama süresi — aydınlatma metnindeki değerle aynı.</summary>
    public static readonly TimeSpan AuditLogRetention = TimeSpan.FromDays(365 * 10);

    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Temizlik işi başarısız olsa da uygulama ayakta kalmalı;
                // bir sonraki turda tekrar denenir.
                logger.LogError(ex, "Saklama süresi temizliği başarısız oldu.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Kapanış sinyali: döngüden normal çıkış.
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTimeOffset.UtcNow;

        // Süresi dolmuş şifre sıfırlama jetonları: soft-delete süzgeci kullanıcı
        // pasife alınmışsa da bu satırları saklamasın diye IgnoreQueryFilters.
        var expiredTokens = await db.PasswordResetTokens
            .IgnoreQueryFilters()
            .Where(t => t.ExpiresAt < now)
            .ToListAsync(ct);

        if (expiredTokens.Count > 0)
            db.PasswordResetTokens.RemoveRange(expiredTokens);

        // 10 yıldan eski denetim izi satırları anonimleştirilir: "ne olду"
        // (Action/EntityType/OccurredAt) kalır, "kim/nereden/tam değer" gider.
        var cutoff = now - AuditLogRetention;

        var oldLogs = await db.AuditLogs
            .Where(a => a.OccurredAt < cutoff
                        && (a.IpAddress != null || a.UserAgent != null
                            || a.BeforeJson != null || a.AfterJson != null))
            .ToListAsync(ct);

        foreach (var log in oldLogs)
        {
            log.IpAddress = null;
            log.UserAgent = null;
            log.BeforeJson = null;
            log.AfterJson = null;
        }

        if (expiredTokens.Count == 0 && oldLogs.Count == 0)
            return;

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Saklama süresi temizliği: {ExpiredTokens} şifre sıfırlama jetonu silindi, "
            + "{AnonymizedLogs} denetim izi satırı anonimleştirildi.",
            expiredTokens.Count, oldLogs.Count);
    }
}
