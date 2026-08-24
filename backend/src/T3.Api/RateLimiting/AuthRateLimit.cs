using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
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
    public static ValueTask OnRejected(OnRejectedContext context, CancellationToken ct)
    {
        var seconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
            ? (int)Math.Ceiling(retryAfter.TotalSeconds)
            : 60;

        context.HttpContext.Response.Headers.RetryAfter =
            seconds.ToString(CultureInfo.InvariantCulture);

        return ValueTask.CompletedTask;
    }

    private static string Ip(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "bilinmeyen-ip";
}
