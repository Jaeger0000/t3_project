using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Paging;
using T3.Application.Common.Rbac;
using T3.Application.Common.Results;
using T3.Domain.Approvals;

namespace T3.Application.Features.Approvals.ListChangeRequests;

/// <summary>
/// Onay kuyruğu. Aynı uç iki iş görür: yetkili için "karara bağlanacak
/// istekler", girişim kullanıcısı için "gönderdiğim isteklerin durumu".
/// Ayrımı <see cref="IChangeRequestScope"/> yapıyor, dolayısıyla iki ayrı uç
/// ve iki ayrı filtre yazmaya gerek kalmıyor.
/// </summary>
public sealed class ListChangeRequestsHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IChangeRequestScope scope)
{
    private sealed record Row(
        Guid Id,
        Guid StartupId,
        string StartupName,
        ChangeTargetType TargetType,
        ChangeOperation Operation,
        Guid? TargetId,
        string PayloadJson,
        string? BeforeJson,
        string SubmittedByName,
        DateTimeOffset SubmittedAt,
        ChangeRequestStatus Status,
        string? ReviewedByName,
        DateTimeOffset? ReviewedAt,
        string? ReviewNote);

    public async Task<Result<ChangeRequestQueueResponse>> Handle(
        ListChangeRequestsRequest request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Error.Forbidden("Oturum bulunamadı.");

        var scoped = scope.Apply(db.ChangeRequests.AsNoTracking());

        if (request.StartupId is { } startupId)
            scoped = scoped.Where(c => c.StartupId == startupId);

        // Durum sayıları süzgeçten önce, tek sorguda: filtre düğmeleri kendi
        // sonuçlarını sıfırlamadan sayı gösterebilsin.
        var counts = await scoped
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, ct);

        var filtered = request.Status is { } status
            ? scoped.Where(c => c.Status == status)
            : scoped;

        var total = await filtered.CountAsync(ct);

        var rows = await filtered
            // Bekleyenler önce: kuyruk bir iş listesi, karara bağlanmış
            // kayıtlar geçmiş. Her blok içinde en yeni üstte.
            .OrderBy(c => c.Status == ChangeRequestStatus.Pending ? 0 : 1)
            .ThenByDescending(c => c.SubmittedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new Row(
                c.Id,
                c.StartupId,
                c.Startup.Name,
                c.TargetType,
                c.Operation,
                c.TargetId,
                c.PayloadJson,
                c.BeforeJson,
                c.SubmittedBy.FullName,
                c.SubmittedAt,
                c.Status,
                c.ReviewedBy != null ? c.ReviewedBy.FullName : null,
                c.ReviewedAt,
                c.ReviewNote))
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;

        var items = rows.Select(row =>
        {
            var body = new ChangeRequestBody(
                row.TargetType, row.Operation, row.PayloadJson, row.BeforeJson);

            // Kuyruk satırı hiçbir alan DEĞERİ taşımıyor; diff yalnızca "kaç
            // alan değişti" için üretiliyor. Bu yüzden bilinçli olarak tümü
            // maskeli görünürlükle çağrılıyor: ileride satıra değer eklenirse
            // varsayılan sızdırmaya değil gizlemeye düşsün.
            var fields = ChangeRequestBodies.Diff(body, StartupVisibility.None);

            return new ChangeRequestListItemResponse(
                row.Id,
                row.StartupId,
                row.StartupName,
                row.TargetType,
                row.Operation,
                row.TargetId,
                ChangeRequestLabels.Target(row.TargetType, ChangeRequestBodies.Subject(body)),
                ChangeRequestLabels.Operation(row.Operation),
                ChangeRequestDiff.ChangedCount(fields),
                row.SubmittedByName,
                row.SubmittedAt,
                row.Status == ChangeRequestStatus.Pending
                    ? Math.Max(0, (int)(now - row.SubmittedAt).TotalDays)
                    : 0,
                row.Status,
                row.ReviewedByName,
                row.ReviewedAt,
                row.ReviewNote);
        }).ToList();

        return new ChangeRequestQueueResponse(
            new PagedResult<ChangeRequestListItemResponse>(
                items, request.Page, request.PageSize, total),
            counts.GetValueOrDefault(ChangeRequestStatus.Pending),
            counts.GetValueOrDefault(ChangeRequestStatus.Approved),
            counts.GetValueOrDefault(ChangeRequestStatus.Rejected));
    }
}
