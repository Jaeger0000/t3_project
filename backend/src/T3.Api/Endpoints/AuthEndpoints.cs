using T3.Api.Filters;
using T3.Api.Http;
using T3.Application.Features.Auth.GetSession;
using T3.Application.Features.Auth.Login;

namespace T3.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Auth");

        auth.MapPost("/login", async (
                LoginRequest request,
                LoginHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(request, ct)).ToHttp())
            .AllowAnonymous()
            .RequireRateLimiting("auth")
            .WithValidation<LoginRequest>()
            .WithSummary("E-posta ve şifreyle giriş yapar, erişim jetonu döner.");

        app.MapGet("/api/me", async (
                GetSessionHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(ct)).ToHttp())
            .WithTags("Auth")
            .WithSummary("Oturum sahibinin kimliğini, rolünü ve yetki kapsamını döner.");

        return app;
    }
}
