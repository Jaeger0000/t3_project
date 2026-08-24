using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Text;
using T3.Infrastructure.Identity;

namespace T3.Api.RateLimiting;

/// <summary>
/// Hız sınırı bölümleme. Bölüm anahtarsız tek kova, bir hesaba yapılan kaba
/// kuvvet denemesinin <b>tüm</b> kullanıcıların girişini 429'a düşürmesi
/// anlamına geliyordu; kilit artık hesap+IP başına.
/// </summary>
public static class AuthRateLimit
{
    public const string AuthPolicy = "auth";
    public const string AiPolicy = "ai";

    private const string EmailItemKey = "auth-email";

    /// <summary>
    /// Giriş isteğinin gövdesindeki e-postayı hız sınırı bölüm anahtarı için
    /// okur. Gövde bir kez okunabildiği için tamponlama açılır ve akış başa
    /// alınır; aksi hâlde model bağlama boş gövde görür.
    /// Hız sınırlayıcının bölüm anahtarı üreten geri çağrısı eşzamanlı (async
    /// değil) olduğundan gövde burada, sınırlayıcıdan önce okunmak zorunda.
    /// </summary>
    public static async Task CaptureLoginEmail(HttpContext context, RequestDelegate next)
    {
        if (HttpMethods.IsPost(context.Request.Method)
            && context.Request.Path.StartsWithSegments("/api/auth")
            && context.Request.ContentLength is > 0 and < 8 * 1024)
        {
            context.Request.EnableBuffering();

            try
            {
                using var document = await JsonDocument.ParseAsync(
                    context.Request.Body, cancellationToken: context.RequestAborted);

                if (document.RootElement.ValueKind == JsonValueKind.Object
                    && document.RootElement.TryGetProperty("email", out var email)
                    && email.ValueKind == JsonValueKind.String)
                {
                    // Kova anahtarı için küçültme yeter: aynı hesabın farklı
                    // yazımları tek kovada toplansın.
                    context.Items[EmailItemKey] = email.GetString()?.ToLowerInvariant();
                }
            }
            catch (JsonException)
            {
                // Bozuk gövde: bölüm anahtarı IP'ye düşer, doğrulama filtresi
                // isteği zaten reddedecek.
            }

            context.Request.Body.Position = 0;
        }

        await next(context);
    }

    /// <summary>Dakikada 10 deneme, IP + e-posta başına.</summary>
    public static RateLimitPartition<string> PartitionAuth(HttpContext context) =>
        RateLimitPartition.GetFixedWindowLimiter(
            $"{Ip(context)}|{context.Items[EmailItemKey] as string ?? "-"}",
            _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 10,
                QueueLimit = 0
            });

    /// <summary>
    /// Dakikada 20 soru, kullanıcı başına: model çağrısı ücretli, bir
    /// kullanıcının panelini açık bırakması diğerlerinin kotasını yemesin.
    /// </summary>
    public static RateLimitPartition<string> PartitionAi(HttpContext context) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst(AppClaims.UserId)?.Value
                ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? Ip(context),
            _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 20,
                QueueLimit = 0
            });

    /// <summary>
    /// Reddedilen isteğe ne kadar sonra tekrar denenebileceğini söyler;
    /// istemci "çok fazla istek" mesajını sayıya çevirebilsin.
    /// </summary>
    public static async ValueTask OnRejected(OnRejectedContext context, CancellationToken ct)
    {
        var seconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
            ? (int)Math.Ceiling(retryAfter.TotalSeconds)
            : 60;

        context.HttpContext.Response.Headers.RetryAfter =
            seconds.ToString(CultureInfo.InvariantCulture);

        await WriteRateLimitAuditAsync(context.HttpContext, seconds, ct);
    }

    /// <summary>
    /// Kilidin kendisini denetim izine yazar — "bu hesap kaba kuvvete uğradı"
    /// bilgisi başarısız deneme satırlarından ayrı bir olaydır.
    ///
    /// Pencere başına <b>tek</b> satır yazılıyor: reddedilen istek sayısı
    /// sınırsız olduğu için her redde satır açmak, saldırganın izi şişirerek
    /// kendi izini boğmasına ya da diski doldurmasına izin verirdi. Bellek
    /// önbelleği bu yüzden burada bir güvenlik önlemi, hız iyileştirmesi değil.
    /// </summary>
    private static async Task WriteRateLimitAuditAsync(
        HttpContext context, int retryAfterSeconds, CancellationToken ct)
    {
        // Yalnızca giriş kovası: AI kotasının dolması güvenlik olayı değil.
        if (!context.Request.Path.StartsWithSegments("/api/auth"))
            return;

        var cache = context.RequestServices.GetService<IMemoryCache>();
        var audit = context.RequestServices.GetService<IAuditWriter>();

        if (cache is null || audit is null)
            return;

        var attemptedEmail = context.Items[EmailItemKey] as string;
        var cacheKey = $"auth-429|{Ip(context)}|{attemptedEmail ?? "-"}";

        if (cache.TryGetValue(cacheKey, out _))
            return;

        cache.Set(cacheKey, true, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1),
            Size = 1
        });

        try
        {
            await audit.WriteForActorAsync(
                actorUserId: null,
                actorRole: null,
                "Auth.RateLimited", "Auth", null,
                after: new
                {
                    Email = MaskedEmail.Of(attemptedEmail),
                    RetryAfterSeconds = retryAfterSeconds
                },
                ct: ct);
        }
        catch (Exception)
        {
            // İz yazılamazsa istek yine 429 dönmeli: reddin kendisi güvenlik
            // önlemi, kaydı ikincil. Yutulan hata ardışık düzenin sonraki
            // adımını etkilemiyor çünkü yanıt gövdesi henüz yazılmadı.
        }
    }

    private static string Ip(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "bilinmeyen-ip";
}
