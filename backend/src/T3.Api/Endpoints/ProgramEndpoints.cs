using T3.Api.Authorization;
using T3.Api.Filters;
using T3.Api.Http;
using T3.Application.Features.Programs.AddParticipation;
using T3.Application.Features.Programs.ListPrograms;

namespace T3.Api.Endpoints;

public static class ProgramEndpoints
{
    public static IEndpointRouteBuilder MapProgramEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/programs", async (
                ListProgramsHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(ct)).ToHttp())
            .WithTags("Programs")
            .WithSummary("Kullanıcının kapsamındaki programları ve dönemlerini listeler.");

        app.MapPost("/api/participations", async (
                AddParticipationRequest request,
                AddParticipationHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(request, ct))
                .ToCreated(created => $"/api/startups/{created.StartupId}/timeline"))
            .WithTags("Programs")
            .RequireAuthorization(Policies.ManageStartups)
            .WithValidation<AddParticipationRequest>()
            .WithSummary("Girişimi bir program dönemine bağlar.");

        return app;
    }
}
