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
        .AllowAnonymous()
        .WithTags("Health")
        .WithSummary("Servisin ayakta olduğunu doğrular.");

        app.MapGet("/health/db", async (AppDbContext db, CancellationToken ct) =>
        {
            var canConnect = await db.Database.CanConnectAsync(ct);

            // Migration ADLARI dönmüyor: şema/sürüm bilgisi anonim bir uçta
            // keşif değeri taşır (bkz. G-12, Guvenlik_Denetimi_ve_Iyilestirme_Plani.md).
            // "Kaç tanesi bekliyor" bilgisi operasyonel izleme için yeterli;
            // hangi migration'ların adı olduğu iç bilgi.
            var pendingCount = canConnect
                ? (await db.Database.GetPendingMigrationsAsync(ct)).Count()
                : 0;

            return canConnect
                ? Results.Ok(new { database = "ok", pendingMigrationCount = pendingCount })
                : Results.Problem("Veritabanına bağlanılamadı.", statusCode: 503);
        })
        .AllowAnonymous()
        .WithTags("Health")
        .WithSummary("Veritabanı bağlantısını ve bekleyen migration sayısını raporlar.");

        return app;
    }
}
