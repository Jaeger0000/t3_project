using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Rbac;
using T3.Application.Common.Results;
using T3.Domain.Approvals;

namespace T3.Application.Features.Approvals.GetChangeRequest;

/// <summary>
/// "Önce / sonra" karşılaştırması — onay akışının karar ekranı.
///
/// KVKK notu: diff, girişim kartıyla aynı maskeleme kurallarından geçer.
/// Aksi hâlde onay ekranı bir kaçış yolu olurdu: kartta vergi numarasını
/// göremeyen Program Yöneticisi, aynı alanı diff satırında okuyabilirdi.
/// </summary>
public sealed class GetChangeRequestHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IChangeRequestScope scope)
{
    public async Task<Result<ChangeRequestDetailResponse>> Handle(Guid id, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Error.Forbidden("Oturum bulunamadı.");

        var request = await scope.Apply(db.ChangeRequests.AsNoTracking())
            .Include(c => c.Startup)
            .Include(c => c.SubmittedBy)
            .Include(c => c.ReviewedBy)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        // Kapsam dışı istek için 404: 403 dönmek isteğin varlığını sızdırır.
        if (request is null)
            return Error.NotFound("Onay isteği bulunamadı.");

        var visibility = StartupVisibility.For(currentUser, request.StartupId);
        var body = ChangeRequestBody.Of(request);
        var fields = ChangeRequestBodies.Diff(body, visibility);

        return new ChangeRequestDetailResponse(
            request.Id,
            request.StartupId,
            request.Startup.Name,
            request.TargetType,
            request.Operation,
            request.TargetId,
            ChangeRequestLabels.Target(request.TargetType, ChangeRequestBodies.Subject(body)),
            ChangeRequestLabels.Operation(request.Operation),
            request.SubmittedBy.FullName,
            request.SubmittedAt,
            request.Status,
            request.ReviewedBy?.FullName,
            request.ReviewedAt,
            request.ReviewNote,
            CanReview: scope.CanReview && request.Status == ChangeRequestStatus.Pending,
            IsReadable: fields.Count > 0,
            ChangedFieldCount: ChangeRequestDiff.ChangedCount(fields),
            Fields: fields,
            RequiresElevation: fields.Any(f => f.Changed && f.Masked));
    }
}
