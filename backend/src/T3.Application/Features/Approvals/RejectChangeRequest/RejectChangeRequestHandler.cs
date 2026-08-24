using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Domain.Approvals;

namespace T3.Application.Features.Approvals.RejectChangeRequest;

/// <summary>
/// Ret: hiçbir veri değişmez, yalnızca istek kapanır ve gerekçe kaydedilir.
/// Kayıt silinmiyor — reddedilen öneriler de denetim izinin parçası ve
/// portalın "önerim neden kabul edilmedi" ekranının kaynağı.
/// </summary>
public sealed class RejectChangeRequestHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IChangeRequestScope scope,
    ChangeRequestApplier applier,
    IAuditWriter audit)
{
    public async Task<Result<ReviewChangeRequestResponse>> Handle(
        Guid id, RejectChangeRequestRequest request, CancellationToken ct)
    {
        if (!scope.CanReview)
            return Error.Forbidden("Onay isteğini reddetme yetkiniz yok.");

        var changeRequest = await scope.Apply(db.ChangeRequests)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (changeRequest is null)
            return Error.NotFound("Onay isteği bulunamadı.");

        if (changeRequest.Status != ChangeRequestStatus.Pending)
            return Error.Conflict(
                changeRequest.Status == ChangeRequestStatus.Approved
                    ? "Bu istek zaten onaylanmış."
                    : "Bu istek zaten reddedilmiş.");

        var reviewedAt = DateTimeOffset.UtcNow;

        changeRequest.Status = ChangeRequestStatus.Rejected;
        changeRequest.ReviewedByUserId = currentUser.UserId;
        changeRequest.ReviewedAt = reviewedAt;
        changeRequest.ReviewNote = request.Note.Trim();

        await db.SaveChangesAsync(ct);

        // Ret kalıcı olduktan sonra öneriyle birlikte gelen geçici kaynaklar
        // bırakılıyor (yüklenmiş ama onaylanmamış dosya). Sıra önemli: önce
        // silip sonra kayıt yazamazsak dosya boşuna gitmiş olurdu.
        await applier.DiscardAsync(changeRequest, ct);

        await audit.WriteAsync(
            "ChangeRequest.Reject", nameof(ChangeRequest), changeRequest.Id,
            after: new
            {
                changeRequest.StartupId,
                changeRequest.TargetType,
                changeRequest.Operation,
                changeRequest.TargetId,
                changeRequest.ReviewNote
            },
            ct: ct);

        return new ReviewChangeRequestResponse(
            changeRequest.Id,
            changeRequest.Status,
            reviewedAt,
            changeRequest.ReviewNote);
    }
}
