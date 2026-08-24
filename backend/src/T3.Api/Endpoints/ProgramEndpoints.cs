using T3.Api.Authorization;
using T3.Api.Filters;
using T3.Api.Http;
using T3.Application.Features.Programs;
using T3.Application.Features.Programs.AddParticipation;
using T3.Application.Features.Programs.CreateProgram;
using T3.Application.Features.Programs.DeleteProgram;
using T3.Application.Features.Programs.ListPrograms;
using T3.Application.Features.Programs.RemoveParticipation;
using T3.Application.Features.Programs.Terms;
using T3.Application.Features.Programs.Terms.AddTerm;
using T3.Application.Features.Programs.Terms.DeleteTerm;
using T3.Application.Features.Programs.Terms.UpdateTerm;
using T3.Application.Features.Programs.UpdateParticipation;
using T3.Application.Features.Programs.UpdateProgram;

namespace T3.Api.Endpoints;

/// <summary>
/// Program uçları. İki farklı politika kullanılıyor: program <em>tanımı</em>
/// yalnızca sistem yöneticisine (kapsamın kendisi), dönem ve katılım
/// işlemleri Program Yöneticisi'ne de açık ama satır düzeyinde kendi
/// programlarıyla sınırlı.
/// </summary>
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

        app.MapPost("/api/programs", async (
                ProgramWriteModel model,
                CreateProgramHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(model, ct))
                .ToCreated(created => $"/api/programs#{created.Id}"))
            .WithTags("Programs")
            .RequireAuthorization(Policies.ManagePrograms)
            .WithValidation<ProgramWriteModel>()
            .WithSummary("Yeni program tanımlar (yalnızca sistem yöneticisi).");

        app.MapPut("/api/programs/{id:guid}", async (
                Guid id,
                ProgramWriteModel model,
                UpdateProgramHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, model, ct)).ToHttp())
            .WithTags("Programs")
            .RequireAuthorization(Policies.ManagePrograms)
            .WithValidation<ProgramWriteModel>()
            .WithSummary("Program tanımını günceller (yalnızca sistem yöneticisi).");

        app.MapDelete("/api/programs/{id:guid}", async (
                Guid id,
                DeleteProgramHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToHttp())
            .WithTags("Programs")
            .RequireAuthorization(Policies.ManagePrograms)
            .WithSummary("Programı ve zincirini pasife alır (yalnızca sistem yöneticisi).");

        app.MapPost("/api/programs/{programId:guid}/terms", async (
                Guid programId,
                ProgramTermWriteModel model,
                AddTermHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(programId, model, ct))
                .ToCreated(created => $"/api/programs#{created.ProgramId}"))
            .WithTags("Programs")
            .RequireAuthorization(Policies.ManageProgramTerms)
            .WithValidation<ProgramTermWriteModel>()
            .WithSummary("Programa dönem ekler (kendi programı).");

        app.MapPut("/api/programs/{programId:guid}/terms/{termId:guid}", async (
                Guid programId,
                Guid termId,
                ProgramTermWriteModel model,
                UpdateTermHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(programId, termId, model, ct)).ToHttp())
            .WithTags("Programs")
            .RequireAuthorization(Policies.ManageProgramTerms)
            .WithValidation<ProgramTermWriteModel>()
            .WithSummary("Dönem bilgilerini günceller (kendi programı).");

        app.MapDelete("/api/programs/{programId:guid}/terms/{termId:guid}", async (
                Guid programId,
                Guid termId,
                DeleteTermHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(programId, termId, ct)).ToHttp())
            .WithTags("Programs")
            .RequireAuthorization(Policies.ManageProgramTerms)
            .WithSummary("Dönemi pasife alır; katılım varsa reddeder.");

        app.MapPost("/api/participations", async (
                AddParticipationRequest request,
                AddParticipationHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(request, ct))
                .ToCreated(created => $"/api/startups/{created.StartupId}/timeline"))
            .WithTags("Programs")
            .RequireAuthorization(Policies.ManageProgramTerms)
            .WithValidation<AddParticipationRequest>()
            .WithSummary("Girişimi bir program dönemine bağlar.");

        app.MapPut("/api/participations/{id:guid}", async (
                Guid id,
                UpdateParticipationRequest request,
                UpdateParticipationHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, request, ct)).ToHttp())
            .WithTags("Programs")
            .RequireAuthorization(Policies.ManageProgramTerms)
            .WithValidation<UpdateParticipationRequest>()
            .WithSummary("Katılım kaydını düzeltir (durum, tarih, not).");

        app.MapDelete("/api/participations/{id:guid}", async (
                Guid id,
                RemoveParticipationHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToHttp())
            .WithTags("Programs")
            .RequireAuthorization(Policies.ManageProgramTerms)
            .WithSummary("Katılım kaydını pasife alır.");

        return app;
    }
}
