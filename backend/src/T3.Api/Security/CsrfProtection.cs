using System.Security.Cryptography;
using System.Text;
using T3.Api.Http;

namespace T3.Api.Security;

/// <summary>
/// Çift-gönderim (double submit) CSRF kontrolü — oturuma bağlı sürüm.
///
/// Jeton çereze taşınınca tarayıcı onu <em>her</em> isteğe ekliyor; başka bir
/// sitedeki form da bizim uca istek kurabilir. <c>SameSite=Strict</c> bunu
/// pratikte kapatıyor ama tek savunma olarak bırakılmıyor: eski tarayıcılar ve
/// ileride gevşetilebilecek bir çerez ayarı kuralı sessizce çürütürdü.
///
/// Beklenen değer artık ayrı bir rastgele çerez değil,
/// <see cref="SessionCookie.DeriveCsrfToken"/> ile oturum jetonundan HMAC
/// olarak türetiliyor: alt alan adından çerez yazabilen bir saldırgan eskiden
/// hem oturum hem CSRF çerezini kendisi koyup kontrolü anlamsızlaştırabilirdi
/// (bkz. G-11, Guvenlik_Denetimi_ve_Iyilestirme_Plani.md).
///
/// Kural yalnızca <b>çerezle kimliklenen yazma isteklerine</b> uygulanıyor:
/// <list type="bullet">
/// <item>Okuma yöntemleri (GET/HEAD/OPTIONS) durumu değiştirmiyor.</item>
/// <item><c>Authorization</c> başlığıyla gelen istekler CSRF'e kapalı — başka
/// bir sitenin sayfası bizim jetonumuzu başlığa koyamaz. Betikler ve MCP
/// istemcileri bu yüzden başlık yolunda kalıyor.</item>
/// </list>
/// </summary>
public static class CsrfProtection
{
    public static async Task Invoke(HttpContext context, RequestDelegate next)
    {
        if (IsSafeMethod(context.Request.Method)
            || HasBearerHeader(context.Request)
            || IsLogout(context.Request)
            || SessionCookie.ReadToken(context) is not { } sessionToken)
        {
            await next(context);
            return;
        }

        // Beklenen değer çerezden okunmuyor, oturum jetonundan yeniden
        // türetiliyor: karşılaştırma "iki çerez birbirine eşit mi" değil,
        // "başlık sunucunun üretebileceği tek değere mi eşit" sorusu oluyor
        // (bkz. G-11, SessionCookie.DeriveCsrfToken).
        var expected = SessionCookie.DeriveCsrfToken(context, sessionToken);
        var header = context.Request.Headers[SessionCookie.CsrfHeaderName].ToString();

        if (!Matches(expected, header))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new ApiErrorBody(
                StatusCodes.Status403Forbidden,
                "İstek doğrulanamadı. Sayfayı yenileyip tekrar deneyin."));
            return;
        }

        await next(context);
    }

    /// <summary>
    /// Çıkış kuralın dışında: zorlanmış bir çıkışın zararı kullanıcının yeniden
    /// giriş yapması. Karşı taraftaki risk daha büyüktü — CSRF çerezi bir
    /// nedenle kaybolduğunda kullanıcı oturumunu <em>kapatamaz</em> hâle gelirdi.
    /// </summary>
    private static bool IsLogout(HttpRequest request) =>
        request.Path.Equals("/api/auth/logout", StringComparison.OrdinalIgnoreCase);

    private static bool IsSafeMethod(string method) =>
        HttpMethods.IsGet(method) || HttpMethods.IsHead(method)
        || HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method);

    private static bool HasBearerHeader(HttpRequest request) =>
        request.Headers.Authorization.ToString()
            .StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Sabit süreli karşılaştırma: uzunluk ya da ilk farklı karakterde erken
    /// çıkmak, saldırganın değeri karakter karakter aramasına kapı açar.
    /// </summary>
    private static bool Matches(string? expected, string? header)
    {
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(header))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(header));
    }
}
