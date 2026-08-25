using System.Security.Cryptography;

namespace T3.Api.Security;

/// <summary>
/// Erişim jetonunun tarayıcı tarafındaki taşıyıcısı.
///
/// Jeton eskiden <c>localStorage</c>'da duruyordu: tek bir XSS açığı Süper
/// Yönetici jetonunu okuyup 32 girişimin vergi numarasını, iletişim bilgisini
/// ve finansallarını dışarı taşımaya yetiyordu. Artık <c>HttpOnly</c> çerezde —
/// script okuyamaz.
///
/// Çerez, jetonu <em>ek olarak</em> taşıyor; <c>Authorization: Bearer</c> yolu
/// kaldırılmadı. Sebep: MCP istemcileri, doğrulama betikleri ve Swagger başlıkla
/// geliyor ve onların çerez kavramı yok. İki yolun ayrımı CSRF kararında da
/// işimize yarıyor (bkz. <see cref="CsrfProtection"/>).
/// </summary>
public static class SessionCookie
{
    public const string Name = "t3.session";

    /// <summary>
    /// CSRF çift-gönderim çerezi. Bilinçli olarak <c>HttpOnly değil</c>:
    /// arayüzün onu okuyup başlığa koyması gerekiyor. Değer bir sır değil,
    /// yalnızca "bu isteği bizim sayfamız kurdu" kanıtı.
    /// </summary>
    public const string CsrfCookieName = "t3.csrf";

    public const string CsrfHeaderName = "X-CSRF-Token";

    public static string? ReadToken(HttpContext context) => context.Request.Cookies[Name];

    /// <summary>Girişte oturum ve CSRF çerezlerini birlikte yazar.</summary>
    public static void Issue(HttpContext context, string token, DateTimeOffset expiresAt)
    {
        context.Response.Cookies.Append(Name, token, new CookieOptions
        {
            HttpOnly = true,
            // Secure isteğin şemasına bağlı: geliştirme makinesinde düz HTTP
            // kullanılıyor ve Secure çerezi tarayıcı hiç saklamazdı. Üretimde
            // TLS zorunlu (Hosting:RequireHttps + HSTS), dolayısıyla true.
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = expiresAt,
            IsEssential = true
        });

        AppendCsrf(context, NewCsrfToken(), expiresAt);
    }

    /// <summary>
    /// Oturum çerezi varken CSRF çerezi yoksa yenisini yazar. Tek amaç
    /// istemciyi kilitli durumda bırakmamak: çerezlerden yalnızca biri
    /// silinirse (tarayıcı temizliği, farklı ömür) her yazma isteği 403 alırdı.
    /// </summary>
    public static void EnsureCsrf(HttpContext context)
    {
        if (ReadToken(context) is null)
            return;

        if (!string.IsNullOrEmpty(context.Request.Cookies[CsrfCookieName]))
            return;

        AppendCsrf(context, NewCsrfToken(), expiresAt: null);
    }

    public static void Clear(HttpContext context)
    {
        var options = new CookieOptions
        {
            Path = "/",
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict
        };

        context.Response.Cookies.Delete(Name, options);
        context.Response.Cookies.Delete(CsrfCookieName, options);
    }

    private static void AppendCsrf(HttpContext context, string value, DateTimeOffset? expiresAt) =>
        context.Response.Cookies.Append(CsrfCookieName, value, new CookieOptions
        {
            HttpOnly = false,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = expiresAt,
            IsEssential = true
        });

    private static string NewCsrfToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
