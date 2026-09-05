using T3.Api.Filters;
using T3.Api.Http;
using T3.Api.RateLimiting;
using T3.Api.Security;
using T3.Application.Common.Interfaces;
using T3.Application.Features.Auth.ChangePassword;
using T3.Application.Features.Auth.GetSession;
using T3.Application.Features.Auth.Login;
using T3.Application.Features.Auth.Register;
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
        //
        // Çerezi silmek başlıkla taşınan bir kopyayı (Authorization: Bearer,
        // Swagger'da veya betikte saklı) etkilemez. SecurityStamp'i de burada
        // yenilemek, bu kullanıcıya ait HER jetonu (bu tarayıcı dâhil tüm
        // cihazlar) geçersiz kılar — ayrı bir oturum/jeton tablosu olmadığı için
        // bilinçli olarak seçilen kısayol bu (bkz. G-01,
        // Guvenlik_Denetimi_ve_Iyilestirme_Plani.md).
        auth.MapPost("/logout", async (
                HttpContext http,
                ICurrentUser currentUser,
                IAppDbContext db,
                IUserStateProvider userState,
                CancellationToken ct) =>
            {
                SessionCookie.Clear(http);

                if (currentUser.UserId is { } userId)
                {
                    var user = await db.Users.FindAsync([userId], ct);
                    if (user is not null)
                    {
                        user.SecurityStamp = Guid.NewGuid();
                        await db.SaveChangesAsync(ct);
                        userState.Invalidate(userId);
                    }
                }

                return Results.NoContent();
            })
            .AllowAnonymous()
            .WithSummary("Oturum çerezlerini siler ve kullanıcının tüm jetonlarını geçersiz kılar.");

        // Kayıt Ol ekranının tek uğrağı: girişim kullanıcısı hiçbir tabloya
        // doğrudan yazamadığı için burası da bir istisna değil, yalnızca onay
        // bekleyen bir başvuru üretir. Herkese açık — ana sayfadan, oturum
        // olmadan çağrılır; kaba kuvvet/numaralandırma riski login ile aynı
        // kovada sınırlanıyor.
        auth.MapPost("/register", async (
                RegisterStartupRequest request,
                RegisterStartupHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(request, ct))
                .ToCreated(created => $"/api/registration-requests/{created.Id}"))
            .AllowAnonymous()
            .RequireRateLimiting(AuthRateLimit.AuthPolicy)
            .WithValidation<RegisterStartupRequest>()
            .WithSummary("Girişim kullanıcısının kendi kendine kayıt başvurusunu oluşturur; SuperAdmin onayı bekler.");

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
                HttpContext http,
                CancellationToken ct) =>
            {
                var result = await handler.Handle(request, ct);

                // Şifre değişince SecurityStamp yenilenir ve bu isteğin kendi
                // çerezi de dâhil eski jetonlar geçersiz olur — kesintisiz
                // sürmesi için hemen yeni jetonla çerez tazeleniyor (bkz. G-01).
                if (result.IsSuccess)
                    SessionCookie.Issue(http, result.Value!.AccessToken, result.Value.ExpiresAt);

                return result.ToHttp();
            })
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
