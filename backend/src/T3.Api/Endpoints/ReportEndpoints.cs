using T3.Api.Http;
using T3.Api.RateLimiting;
using T3.Application.Features.Reports.EcosystemStats;
using T3.Application.Features.Reports.ExportStartups;
using T3.Domain.Startups;

namespace T3.Api.Endpoints;

/// <summary>
/// Karar destek raporları (Faz 5). Politika yok — her rol kendi kapsamının
/// karnesini görür; daraltmayı IStartupScope, maskelemeyi StartupVisibility
/// yapar. Ayrı bir "rapor yetkisi" tanımlamak üçüncü bir kural noktası
/// yaratırdı.
/// </summary>
public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports").WithTags("Reports");

        group.MapGet("/ecosystem", async (
                Guid? programId,
                Sector? sector,
                string? city,
                int? year,
                EcosystemStatsHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(new EcosystemStatsRequest(programId, sector, city, year), ct)).ToHttp())
            .WithSummary("Ekosistem karnesi: sayımlar, dağılımlar ve finansal toplamlar.");

        group.MapGet("/export", async (
                string? q,
                Sector? sector,
                StartupStatus? status,
                Guid? programId,
                string? city,
                ExportStartupsHandler handler,
                HttpContext http,
                CancellationToken ct) =>
            {
                var result = await handler.Handle(
                    new ExportStartupsRequest(q, sector, status, programId, city), ct);

                if (!result.IsSuccess)
                    return ApiResults.Problem(result.Error!);

                var file = result.Value!;

                // Tarayıcı içerik türünü tahmin etmeye çalışmasın: CSV metin
                // dosyası olduğu için tahmin, HTML olarak yorumlanma riski taşır.
                http.Response.Headers["X-Content-Type-Options"] = "nosniff";

                return Results.File(file.Content, file.ContentType, file.FileName);
            })
            .RequireRateLimiting(AuthRateLimit.MassExportPolicy)
            .WithSummary("Süzülmüş girişim listesini CSV olarak indirir (maskeleme korunur).");

        return app;
    }
}
