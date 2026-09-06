using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Text;
using T3.Domain.Identity;
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

    /// <summary>
    /// Kütlesel veri çekme yüzeyleri: CSV aktarımı, doküman indirme, MCP.
    /// Ele geçirilmiş tek bir jeton bunlardan biriyle dakikalar içinde
    /// kapsamındaki her şeyi dışarı taşıyabiliyordu — hiçbir şey yavaşlatmıyor,
    /// hiçbir şey uyarmıyordu (bkz. G-07, Guvenlik_Denetimi_ve_Iyilestirme_Plani.md).
    /// </summary>
    public const string MassExportPolicy = "mass-export";

    /// <summary>
    /// AI raporu üretimi tek başına bir soru değil — her bölüm ayrı bir model
    /// çağrısı tetikliyor (6 standart bölüm + varsa özel istek), yani tek bir
    /// tıklama bile OpenRouter'a birden çok istek gönderiyor.
    /// <see cref="AiPolicy"/>'nin dakikalık kotası bunu hesaba katmıyor,
    /// <see cref="MassExportPolicy"/> ise farklı bir tehdit modeline (kütlesel
    /// veri çekme) ait; bu yüzden ayrı ve daha sıkı bir kova.
    /// </summary>
    public const string AiReportPolicy = "ai-report";

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
    /// Dakikada 30 deneme, yalnızca IP başına — e-posta bölümlemesinin
    /// açıkta bıraktığı yüzeyi kapatır: tek bir IP'den 1.000 farklı e-postaya
    /// dakikada 10.000 deneme (parola serpiştirme) artık bu kovaya takılır.
    /// <see cref="ChainedAuthLimiter"/> ile <see cref="PartitionAuth"/>'a
    /// zincirlenmiş durumda — ikisinden biri dolarsa istek reddedilir
    /// (bkz. G-05, Guvenlik_Denetimi_ve_Iyilestirme_Plani.md).
    /// </summary>
    public static RateLimitPartition<string> PartitionAuthIp(HttpContext context) =>
        RateLimitPartition.GetFixedWindowLimiter(
            Ip(context),
            _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 30,
                QueueLimit = 0
            });

    /// <summary>
    /// <c>options.AddPolicy</c> tek bir partitioner alıyor; iki bağımsız kovayı
    /// (hesap+IP ile yalnızca-IP) aynı adlı politika altında birleştirmenin
    /// yolu yok. Bunun yerine bu limiter <see cref="EnforceIpLimit"/>
    /// middleware'i içinde <c>UseRateLimiter()</c>'dan bağımsız, elle
    /// çalıştırılıyor — istek ikisinden birine takılırsa 429 döner.
    /// </summary>
    private static readonly PartitionedRateLimiter<HttpContext> IpOnlyLimiter =
        PartitionedRateLimiter.Create<HttpContext, string>(PartitionAuthIp);

    /// <summary>
    /// Yalnızca-IP kovasını <c>/api/auth</c> uçları için elle uygular. Named
    /// policy'lerin (<see cref="AuthPolicy"/>) dışında tutulmasının sebebi
    /// yukarıdaki not. <see cref="CaptureLoginEmail"/>'den hemen sonra,
    /// <c>UseRateLimiter()</c>'dan önce çalışır.
    /// </summary>
    public static async Task EnforceIpLimit(HttpContext context, RequestDelegate next)
    {
        if (!HttpMethods.IsPost(context.Request.Method)
            || !context.Request.Path.StartsWithSegments("/api/auth"))
        {
            await next(context);
            return;
        }

        using var lease = await IpOnlyLimiter.AcquireAsync(context, cancellationToken: context.RequestAborted);

        if (lease.IsAcquired)
        {
            await next(context);
            return;
        }

        var seconds = lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
            ? (int)Math.Ceiling(retryAfter.TotalSeconds)
            : 60;

        context.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
        await WriteRateLimitAuditAsync(context, seconds, context.RequestAborted);

        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.Response.WriteAsJsonAsync(new { status = 429, title = "Çok fazla istek." });
    }

    /// <summary>
    /// Saatte 10 kütlesel çekme (CSV aktarımı, doküman indirme, MCP isteği),
    /// kullanıcı başına. Kimliksiz istek buraya düşmez (uçların hepsi kimlik
    /// istiyor); yine de düşerse IP'ye bölümlenir.
    /// </summary>
    public static RateLimitPartition<string> PartitionMassExport(HttpContext context) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst(AppClaims.UserId)?.Value ?? Ip(context),
            _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromHours(1),
                PermitLimit = 10,
                QueueLimit = 0
            });

    /// <summary>Saatte 6 rapor, kullanıcı başına.</summary>
    public static RateLimitPartition<string> PartitionAiReport(HttpContext context) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst(AppClaims.UserId)?.Value ?? Ip(context),
            _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromHours(1),
                PermitLimit = 6,
                QueueLimit = 0
            });

    /// <summary>
    /// MCP'ye özel kova: <c>/mcp</c> tek bir uçtan hem sıradan sorular hem
    /// <c>tools/call</c> döngüleri geçiyor. <see cref="MassExportPolicy"/>'nin
    /// saatte 10'u burada gerçek kullanımı (Demo Day'de jürinin Claude
    /// Desktop'tan birden çok soru sorması) kırardı; bunun yerine dakikada
    /// 100 — sıradan kullanımın çok üstünde, koşan bir betiğin dakikalar
    /// içinde tüm ekosistemi çekmesinin çok altında.
    /// </summary>
    public const string McpPolicy = "mcp";

    public static RateLimitPartition<string> PartitionMcp(HttpContext context) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst(AppClaims.UserId)?.Value ?? Ip(context),
            _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 100,
                QueueLimit = 0
            });

    /// <summary>
    /// Genel kova: kimliği olan her kullanıcı için dakikada 300 istek üst
    /// sınırı. <c>options.GlobalLimiter</c>'a bağlanır ve tüm isteklere
    /// (isim verilmiş politikalara ek olarak) uygulanır — adı konmamış
    /// uçlarda bile bir üst sınır olsun diye (bkz. G-07).
    /// </summary>
    public static RateLimitPartition<string> PartitionGlobal(HttpContext context) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst(AppClaims.UserId)?.Value ?? Ip(context),
            _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 300,
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
        await WriteMassExportAuditAsync(context.HttpContext, seconds, ct);
    }

    /// <summary>
    /// Kütlesel çekme kovalarından biri dolduğunda ayrı ve isimli bir iz
    /// düşer — "bir kullanıcı normalin çok üstünde veri çekmeye çalıştı"
    /// bilgisi, sıradan bir 429'dan farklı bir güvenlik sinyali (bkz. G-07).
    /// </summary>
    private static async Task WriteMassExportAuditAsync(
        HttpContext context, int retryAfterSeconds, CancellationToken ct)
    {
        var isMassExportSurface =
            context.Request.Path.StartsWithSegments("/api/reports/export")
            || context.Request.Path.StartsWithSegments("/api/documents")
            || context.Request.Path.StartsWithSegments("/mcp")
            || context.Request.Path.Value?.EndsWith("/ai-report", StringComparison.Ordinal) is true;

        if (!isMassExportSurface)
            return;

        var audit = context.RequestServices.GetService<IAuditWriter>();
        if (audit is null)
            return;

        var userId = Guid.TryParse(context.User.FindFirst(AppClaims.UserId)?.Value, out var id)
            ? id : (Guid?)null;
        var role = Enum.TryParse<UserRole>(
            context.User.FindFirst(AppClaims.Role)?.Value, out var parsedRole)
            ? parsedRole : (UserRole?)null;

        try
        {
            await audit.WriteForActorAsync(
                userId, role,
                "Security.MassExport", "Security", null,
                after: new
                {
                    Path = context.Request.Path.Value,
                    RetryAfterSeconds = retryAfterSeconds
                },
                ct: ct);
        }
        catch (Exception)
        {
            // bkz. WriteRateLimitAuditAsync: iz ikincil, 429 yanıtı birincil.
        }
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
