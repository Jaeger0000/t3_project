using T3.Api.Filters;
using T3.Api.Http;
using T3.Api.RateLimiting;
using T3.Api.Security;
using T3.Application.Features.Auth.ChangePassword;
using T3.Application.Features.Auth.GetSession;
using T3.Application.Features.Auth.Login;
using T3.Application.Features.Auth.RequestPasswordReset;
using T3.Application.Features.Auth.ResetPassword;

namespace T3.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Auth");

        auth.MapPost("/login", async (
                LoginRequest request,
                LoginHandler handler,
                HttpContext http,
                CancellationToken ct) =>
            {
                var result = await handler.Handle(request, ct);

                // Tarayıcı jetonu HttpOnly çerezden kullanır — script okuyamaz.
                // Gövdedeki jeton kalıyor çünkü betikler, MCP istemcileri ve
                // Swagger çerez taşımıyor; ikisi aynı jeton, iki taşıma yolu.
                if (result.IsSuccess)
                    SessionCookie.Issue(http, result.Value!.AccessToken, result.Value.ExpiresAt);

                return result.ToHttp();
            })
            .AllowAnonymous()
            .RequireRateLimiting(AuthRateLimit.AuthPolicy)
            .WithValidation<LoginRequest>()
            .WithSummary("E-posta ve şifreyle giriş yapar; jetonu HttpOnly çerezde ve gövdede döner.");

        // Çıkış sunucu tarafında çerezi siliyor: istemcinin "unutması" yetmez,
        // çerezi yalnızca sunucu geçersiz kılabilir. Kimlik istemiyor — süresi
        // dolmuş jetonu olan kullanıcı da çerezini temizleyebilmeli.
        auth.MapPost("/logout", (HttpContext http) =>
            {
                SessionCookie.Clear(http);
                return Results.NoContent();
            })
            .AllowAnonymous()
            .WithSummary("Oturum çerezlerini siler.");

        // Kurtarma uçları da giriş kovasında: ikisi de kimlik doğrulamadan önce
        // e-posta alan, kaba kuvvete ve numaralandırmaya açık yüzeyler.
        auth.MapPost("/forgot-password", async (
                RequestPasswordResetRequest request,
                RequestPasswordResetHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(request, ct)).ToHttp())
            .AllowAnonymous()
            .RequireRateLimiting(AuthRateLimit.AuthPolicy)
            .WithValidation<RequestPasswordResetRequest>()
            .WithSummary("Şifre sıfırlama bağlantısı gönderir (yanıt adresin kayıtlı olup olmadığını söylemez).");

        auth.MapPost("/reset-password", async (
                ResetPasswordRequest request,
                ResetPasswordHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(request, ct)).ToHttp())
            .AllowAnonymous()
            .RequireRateLimiting(AuthRateLimit.AuthPolicy)
            .WithValidation<ResetPasswordRequest>()
            .WithSummary("Sıfırlama jetonuyla yeni şifre belirler; jeton tek kullanımlıktır.");

        auth.MapPost("/change-password", async (
                ChangePasswordRequest request,
                ChangePasswordHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(request, ct)).ToHttp())
            .RequireRateLimiting(AuthRateLimit.AuthPolicy)
            .WithValidation<ChangePasswordRequest>()
            .WithSummary("Oturum sahibinin şifresini değiştirir; mevcut şifre doğrulanır.");

        app.MapGet("/api/me", async (
                GetSessionHandler handler,
                HttpContext http,
                CancellationToken ct) =>
            {
                // Oturum çerezi varken CSRF çerezi kaybolmuşsa istemci hiçbir
                // yazma isteği yapamaz hâle gelir; açılışta sessizce onarılıyor.
                SessionCookie.EnsureCsrf(http);
                return (await handler.Handle(ct)).ToHttp();
            })
            .WithTags("Auth")
            .WithSummary("Oturum sahibinin kimliğini, rolünü ve yetki kapsamını döner.");

        return app;
    }
}
