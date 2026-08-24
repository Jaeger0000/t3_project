using T3.Application.Common.Paging;
using T3.Domain.Approvals;

namespace T3.Application.Features.Approvals.ListChangeRequests;

public sealed record ListChangeRequestsRequest : PagedRequest
{
    public ChangeRequestStatus? Status { get; init; }

    /// <summary>Tek bir girişimin istek geçmişini süzmek için.</summary>
    public Guid? StartupId { get; init; }
}

public sealed record ChangeRequestListItemResponse(
    Guid Id,
    Guid StartupId,
    string StartupName,
    ChangeTargetType TargetType,
    ChangeOperation Operation,
    Guid? TargetId,
    string TargetLabel,
    string OperationLabel,
    int ChangedFieldCount,
    string SubmittedByName,
    DateTimeOffset SubmittedAt,
    int WaitingDays,
    ChangeRequestStatus Status,
    string? ReviewedByName,
    DateTimeOffset? ReviewedAt,
    string? ReviewNote);

/// <summary>
/// Kuyruk yanıtı. Durum sayıları, hangi durum süzgeci uygulanmış olursa olsun
/// tüm kapsam üzerinden hesaplanır: filtre düğmeleri "Bekleyen (6)" gibi
/// sayıları kendi isteklerini atmadan gösterebilsin.
/// </summary>
public sealed record ChangeRequestQueueResponse(
    PagedResult<ChangeRequestListItemResponse> Page,
    int PendingCount,
    int ApprovedCount,
    int RejectedCount);
