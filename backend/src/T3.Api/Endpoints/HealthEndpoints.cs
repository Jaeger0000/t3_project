using Microsoft.EntityFrameworkCore;
using T3.Infrastructure.Persistence;

namespace T3.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new
        {
            status = "ok",
            service = "T3 Girişim Ekosistemi API",
            time = DateTimeOffset.UtcNow
        }))
        .WithTags("Health")
        .WithSummary("Servisin ayakta olduğunu doğrular.");

        app.MapGet("/health/db", async (AppDbContext db, CancellationToken ct) =>
        {
            var canConnect = await db.Database.CanConnectAsync(ct);
            var pending = (await db.Database.GetPendingMigrationsAsync(ct)).ToArray();

            return canConnect
                ? Results.Ok(new { database = "ok", pendingMigrations = pending })
                : Results.Problem("Veritabanına bağlanılamadı.", statusCode: 503);
        })
        .WithTags("Health")
        .WithSummary("Veritabanı bağlantısını ve bekleyen migration'ları raporlar.");

        return app;
    }
}
