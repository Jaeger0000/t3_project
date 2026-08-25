namespace T3.Api.Security;

/// <summary>
/// Yanıt güvenlik başlıkları — jeton çereze taşındığı için CSP artık ikinci
/// savunma değil, ilk savunmanın parçası: <c>HttpOnly</c> çerez script'in
/// jetonu <em>okumasını</em> engelliyor, CSP ise yabancı script'in çalışmasını.
///
/// Politika tek origin varsayımı üzerine kurulu (bkz. 2.1): arayüz API ile aynı
/// kökten geliyor, dolayısıyla <c>'self'</c> her şey için yeterli ve harici CDN
/// yok.
/// </summary>
public static class SecurityHeaders
{
    private const string Policy =
        "default-src 'self'; " +
        "script-src 'self'; " +
        // style-src'de 'unsafe-inline' zorunlu: grafik kütüphanesi (Recharts)
        // ölçüleri element style özniteliğine yazıyor. Script tarafı açık
        // değil — XSS'in tehlikeli kolu orası.
        "style-src 'self' 'unsafe-inline'; " +
        // blob: indirme akışı için: dosya içeriği jetonla fetch ediliyor ve
        // geçici bir nesne URL'sinden kaydediliyor (bkz. apiClient.download).
        "img-src 'self' data: blob:; " +
        "font-src 'self' data:; " +
        "connect-src 'self'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'";

    public static async Task Invoke(HttpContext context, RequestDelegate next)
    {
        var response = context.Response;
        var isApi = context.Request.Path.StartsWithSegments("/api");

        // Başlıklar yanıt gövdesi yazılmaya başlamadan önce eklenmeli.
        response.OnStarting(() =>
        {
            response.Headers["X-Content-Type-Options"] = "nosniff";
            response.Headers["Referrer-Policy"] = "no-referrer";
            response.Headers["X-Frame-Options"] = "DENY";
            response.Headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";
            response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";

            // Swagger arayüzü kendi script/stil bloklarını sayfaya gömüyor;
            // sıkı politika onu boş ekrana çevirir. Yalnızca geliştirmede açık
            // olduğu için istisna dar tutuluyor.
            if (!context.Request.Path.StartsWithSegments("/swagger"))
                response.Headers["Content-Security-Policy"] = Policy;

            // API yanıtları önbelleğe girmemeli: içlerinde rol bazlı maskelenmiş
            // kişisel veri ve indirilen belgeler var. Paylaşılan bir makinede
            // "geri" tuşuyla başka kullanıcının verisine dönmek bu başlıkla
            // kapanıyor.
            if (isApi)
            {
                response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
                response.Headers.Pragma = "no-cache";
            }

            return Task.CompletedTask;
        });

        await next(context);
    }
}
