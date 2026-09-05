using T3.Api.Http;
using T3.Application.Common.Interfaces;

namespace T3.Api.Middleware;

/// <summary>
/// <c>User.MustChangePassword</c> bugüne kadar yalnızca arayüzdeki rota
/// koruyucusuna dayanıyordu: geçici şifreyle giriş yapan kullanıcı
/// <c>Authorization: Bearer</c> ile şifresini hiç değiştirmeden tüm REST
/// yüzeyini kullanabiliyordu (bkz. G-06, Guvenlik_Denetimi_ve_Iyilestirme_Plani.md).
///
/// Bir middleware olarak yazılmasının sebebi <c>/mcp</c>'yi de kapsaması
/// gerekmesi: tek tek uç noktalara eklenen bir yetkilendirme politikası ya
/// da endpoint filtresi yalnızca o rotaya bağlıdır, MCP tüm araçlarını tek
/// bir <c>/mcp</c> isteği içinde dağıttığı için atlanır. Middleware ise her
/// isteğin (route'undan bağımsız) üzerinden geçer.
///
/// <see cref="UserStateMiddleware"/>'den hemen sonra çalışır ve onun
/// bıraktığı durumu kullanır — ikinci bir sorgu atmaz.
/// </summary>
public sealed class MustChangePasswordMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Bayrak açıkken serbest kalan tek yüzey: oturumu kapatmak, kendi
    /// kimliğini görmek (arayüzün karar vermesi için) ve şifreyi değiştirmek.
    /// </summary>
    private static readonly string[] AllowedPrefixes =
    [
        "/api/me",
        "/api/auth/change-password",
        "/api/auth/logout",
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api")
            || AllowedPrefixes.Any(prefix => context.Request.Path.StartsWithSegments(prefix)))
        {
            await next(context);
            return;
        }

        if (context.Items[UserStateMiddleware.UserStateItemKey] is UserState { MustChangePassword: true })
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new ApiErrorBody(
                StatusCodes.Status403Forbidden,
                "Devam etmek için şifrenizi değiştirmelisiniz."));
            return;
        }

        await next(context);
    }
}
