using T3.Api.Authorization;
using T3.Api.Filters;
using T3.Api.Http;
using T3.Application.Features.Approvals.ApproveChangeRequest;
using T3.Application.Features.Approvals.GetChangeRequest;
using T3.Application.Features.Approvals.ListChangeRequests;
using T3.Application.Features.Approvals.RejectChangeRequest;
using T3.Application.Features.Approvals.SubmitChangeRequest;
using T3.Domain.Approvals;

namespace T3.Api.Endpoints;

/// <summary>
/// Onay akışı uçları (MVP #3). Gönderme ucu politika ile korunmuyor: gönderme
/// yetkisi role değil girişim bağına dayanıyor ve karar
/// <c>IChangeRequestScope.CanSubmit</c>'te veriliyor. Karar uçları ise kaba
/// yetki kapısını da geçmek zorunda — politika ilk savunma, kapsam ikinci.
/// </summary>
public static class ApprovalEndpoints
{
    public static IEndpointRouteBuilder MapApprovalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/change-requests").WithTags("Approvals");

        group.MapPost("/", async (
                SubmitChangeRequestRequest request,
                SubmitChangeRequestHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(request, ct))
                .ToCreated(created => $"/api/change-requests/{created.Id}"))
            .WithValidation<SubmitChangeRequestRequest>()
            .WithSummary("Girişim kullanıcısının değişiklik önerisini kuyruğa alır.");

        // Aynı uç iki iş görür: yetkili için onay kuyruğu, girişim kullanıcısı
        // için "gönderdiğim isteklerin durumu". Ayrımı kapsam yapıyor.
        group.MapGet("/", async (
                ChangeRequestStatus? status,
                Guid? startupId,
                int? page,
                int? pageSize,
                ListChangeRequestsHandler handler,
                CancellationToken ct) =>
            {
                var request = new ListChangeRequestsRequest
                {
                    Status = status,
                    StartupId = startupId,
                    Page = page ?? 1,
                    PageSize = pageSize ?? 20
                };

                return (await handler.Handle(request, ct)).ToHttp();
            })
            .WithSummary("Onay kuyruğunu döner; durum sayıları filtreden bağımsız hesaplanır.");

        group.MapGet("/{id:guid}", async (
                Guid id,
                GetChangeRequestHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToHttp())
            .WithSummary("Önerinin önce/sonra karşılaştırmasını döner (alanlar role göre maskelenir).");

        group.MapPost("/{id:guid}/approve", async (
                Guid id,
                ApproveChangeRequestRequest request,
                ApproveChangeRequestHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, request, ct)).ToHttp())
            .RequireAuthorization(Policies.ReviewApprovals)
            .WithValidation<ApproveChangeRequestRequest>()
            .WithSummary("Öneriyi onaylar ve hedef kayda uygular.");

        group.MapPost("/{id:guid}/reject", async (
                Guid id,
                RejectChangeRequestRequest request,
                RejectChangeRequestHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, request, ct)).ToHttp())
            .RequireAuthorization(Policies.ReviewApprovals)
            .WithValidation<RejectChangeRequestRequest>()
            .WithSummary("Öneriyi gerekçesiyle reddeder; hiçbir veri değişmez.");

        return app;
    }
}
