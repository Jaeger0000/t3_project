using T3.Api.Authorization;
using T3.Api.Filters;
using T3.Api.Http;
using T3.Application.Features.Registrations.ApproveRegistrationRequest;
using T3.Application.Features.Registrations.ListRegistrationRequests;
using T3.Application.Features.Registrations.RejectRegistrationRequest;
using T3.Domain.Registrations;

namespace T3.Api.Endpoints;

/// <summary>
/// Kayıt Ol formundan gelen başvuruların yönetimi — yetki matrisinde yalnızca
/// SuperAdmin'e açık (kullanıcı yönetimiyle aynı politika: yeni bir hesap
/// açmanın son adımı bu uç, ManageUsers'tan ayrı bir politika icat etmeye
/// gerek yok).
/// </summary>
public static class RegistrationEndpoints
{
    public static IEndpointRouteBuilder MapRegistrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/registration-requests")
            .WithTags("Registrations")
            .RequireAuthorization(Policies.ManageUsers);

        group.MapGet("/", async (
                RegistrationRequestStatus? status,
                int? page,
                int? pageSize,
                ListRegistrationRequestsHandler handler,
                CancellationToken ct) =>
            {
                var request = new ListRegistrationRequestsRequest
                {
                    Status = status,
                    Page = page ?? 1,
                    PageSize = pageSize ?? 20
                };

                return (await handler.Handle(request, ct)).ToHttp();
            })
            .WithSummary("Kayıt Ol başvurularını listeler ve duruma göre süzer.");

        group.MapPost("/{id:guid}/approve", async (
                Guid id,
                ApproveRegistrationRequestHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToHttp())
            .WithSummary("Başvuruyu onaylar; gerçek Startup ve StartupUser hesabı burada doğar.");

        group.MapPost("/{id:guid}/reject", async (
                Guid id,
                RejectRegistrationRequestRequest request,
                RejectRegistrationRequestHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, request, ct)).ToNoContent())
            .WithValidation<RejectRegistrationRequestRequest>()
            .WithSummary("Başvuruyu gerekçesiyle reddeder; hiçbir hesap oluşturulmaz.");

        return app;
    }
}
