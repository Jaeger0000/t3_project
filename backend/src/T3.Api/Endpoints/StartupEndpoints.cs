using T3.Api.Authorization;
using T3.Api.Filters;
using T3.Api.Http;
using T3.Application.Features.Startups;
using T3.Application.Features.Startups.CreateStartup;
using T3.Application.Features.Startups.GetStartupCard;
using T3.Application.Features.Startups.GetStartupTimeline;
using T3.Application.Features.Startups.SearchStartups;
using T3.Application.Features.Startups.Team;
using T3.Application.Features.Startups.Team.AddTeamMember;
using T3.Application.Features.Startups.Team.RemoveTeamMember;
using T3.Application.Features.Startups.Team.UpdateTeamMember;
using T3.Application.Features.Startups.UpdateStartup;
using T3.Domain.Startups;

namespace T3.Api.Endpoints;

public static class StartupEndpoints
{
    public static IEndpointRouteBuilder MapStartupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/startups").WithTags("Startups");

        // --- Okuma ---------------------------------------------------------
        // Yetki daraltması handler içindeki IStartupScope'ta; her rol bu ucu
        // çağırabilir ama farklı satır kümesi görür.
        group.MapGet("/", async (
                string? q,
                Sector? sector,
                StartupStatus? status,
                Guid? programId,
                string? city,
                StartupSort? sort,
                int? page,
                int? pageSize,
                SearchStartupsHandler handler,
                CancellationToken ct) =>
            {
                // Sayfalama parametreleri nullable: lambda'da varsayılan değer
                // handler ve CancellationToken'dan önce gelemez, sınırlar zaten
                // PagedRequest içinde kırpılıyor.
                var request = new SearchStartupsRequest
                {
                    Q = q,
                    Sector = sector,
                    Status = status,
                    ProgramId = programId,
                    City = city,
                    Sort = sort ?? StartupSort.Name,
                    Page = page ?? 1,
                    PageSize = pageSize ?? 20
                };

                return (await handler.Handle(request, ct)).ToHttp();
            })
            .WithSummary("Girişimleri arar ve filtreler; sonuçlar role göre daraltılır.");

        group.MapGet("/{id:guid}", async (
                Guid id,
                GetStartupCardHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToHttp())
            .WithSummary("Merkezi girişim kartını döner (hassas alanlar role göre maskelenir).");

        group.MapGet("/{id:guid}/timeline", async (
                Guid id,
                GetStartupTimelineHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToHttp())
            .WithSummary("Girişimin gelişim yolculuğunu kronolojik olarak döner.");

        // --- Yazma ---------------------------------------------------------
        group.MapPost("/", async (
                StartupWriteModel model,
                CreateStartupHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(model, ct))
                .ToCreated(created => $"/api/startups/{created.Id}"))
            .RequireAuthorization(Policies.ManageStartups)
            .WithValidation<StartupWriteModel>()
            .WithSummary("Yeni girişim kaydı oluşturur.");

        group.MapPut("/{id:guid}", async (
                Guid id,
                StartupWriteModel model,
                UpdateStartupHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, model, ct)).ToHttp())
            .RequireAuthorization(Policies.ManageStartups)
            .WithValidation<StartupWriteModel>()
            .WithSummary("Girişim kaydını günceller.");

        // --- Ekip ----------------------------------------------------------
        group.MapPost("/{id:guid}/team", async (
                Guid id,
                TeamMemberWriteModel model,
                AddTeamMemberHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, model, ct))
                .ToCreated(member => $"/api/startups/{id}/team/{member.Id}"))
            .RequireAuthorization(Policies.ManageStartups)
            .WithValidation<TeamMemberWriteModel>()
            .WithSummary("Girişime ekip üyesi ekler.");

        group.MapPut("/{id:guid}/team/{memberId:guid}", async (
                Guid id,
                Guid memberId,
                TeamMemberWriteModel model,
                UpdateTeamMemberHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, memberId, model, ct)).ToHttp())
            .RequireAuthorization(Policies.ManageStartups)
            .WithValidation<TeamMemberWriteModel>()
            .WithSummary("Ekip üyesini günceller.");

        group.MapDelete("/{id:guid}/team/{memberId:guid}", async (
                Guid id,
                Guid memberId,
                RemoveTeamMemberHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, memberId, ct)).ToNoContent())
            .RequireAuthorization(Policies.ManageStartups)
            .WithSummary("Ekip üyesini pasife alır (kayıt silinmez, işaretlenir).");

        return app;
    }
}
