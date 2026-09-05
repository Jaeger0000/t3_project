using T3.Api.Http;
using T3.Application.Common.Interfaces;
using T3.Infrastructure.Identity;

namespace T3.Api.Middleware;

/// <summary>
/// Jetonun içindeki kimlik/rol/güvenlik damgasını her istekte veritabanındaki
/// canlı değerle karşılaştırır. RBAC'ın tamamı jetonun claim'lerine dayandığı
/// için "yetkiyi geri alma" burada olmadan yalnızca yeni girişi engelliyordu —
/// elinde eski jetonu olan pasife alınmış/rolü düşürülmüş bir kullanıcı jeton
/// süresi dolana kadar tam yetkili kalıyordu (bkz. G-01,
/// Guvenlik_Denetimi_ve_Iyilestirme_Plani.md).
///
/// <c>UseAuthentication</c>'dan sonra, <c>UseAuthorization</c>'dan önce
/// çalışır: geçersiz durumda 401 ile kısa devre yapar, hangi endpoint'in
/// hangi rolü istediğine hiç bakmaz.
/// </summary>
public sealed class UserStateMiddleware(RequestDelegate next)
{
    /// <summary>bkz. <see cref="MustChangePasswordMiddleware"/>.</summary>
    public const string UserStateItemKey = "t3:userstate";

    public async Task InvokeAsync(HttpContext context, IUserStateProvider userState)
    {
        var principal = context.User;

        if (principal.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var userIdClaim = principal.FindFirst(AppClaims.UserId)?.Value;
        var roleClaim = principal.FindFirst(AppClaims.Role)?.Value;
        var stampClaim = principal.FindFirst(AppClaims.SecurityStamp)?.Value;

        // Bu claim'ler her zaman JwtTokenService'in ürettiği jetonlarda birlikte
        // bulunur; eksikse jeton bu sürümden önce üretilmiş demektir — eski
        // biçim de reddedilsin.
        if (!Guid.TryParse(userIdClaim, out var userId)
            || !Guid.TryParse(stampClaim, out var stamp)
            || roleClaim is null)
        {
            await WriteUnauthorizedAsync(context, "Oturum jetonu geçersiz. Lütfen yeniden giriş yapın.");
            return;
        }

        var state = await userState.GetAsync(userId, context.RequestAborted);

        var isValid = state is { IsActive: true } live
            && live.SecurityStamp == stamp
            && live.Role.ToString() == roleClaim;

        if (!isValid)
        {
            await WriteUnauthorizedAsync(context, "Oturumunuz artık geçerli değil. Lütfen yeniden giriş yapın.");
            return;
        }

        // MustChangePasswordMiddleware aynı sorguyu tekrar atmasın diye canlı
        // durum burada bırakılıyor — ikisi de aynı önbellek girdisine bakıyor
        // olsa da istek başına tek DB/önbellek erişimi yeterli.
        context.Items[UserStateItemKey] = state;

        await next(context);
    }

    private static Task WriteUnauthorizedAsync(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return context.Response.WriteAsJsonAsync(
            new ApiErrorBody(StatusCodes.Status401Unauthorized, message));
    }
}
