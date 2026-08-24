using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Domain.Approvals;

namespace T3.Application.Features.Approvals.ApproveChangeRequest;

/// <summary>
/// Onay: önerinin hedef varlığa uygulandığı tek nokta. MVP #3'ün kanıtı —
/// girişim verisi yalnızca buradan yayına girer.
///
/// Uygulama ile durum güncellemesi tek <c>SaveChanges</c>'ta kalıcı olur.
/// Ayrılsalardı "onaylandı ama uygulanmadı" ya da tersi bir kayıt oluşabilir,
/// kuyruk ile veri birbirine yalan söylerdi.
/// </summary>
public sealed class ApproveChangeRequestHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IChangeRequestScope scope,
    ChangeRequestApplier applier,
    IAuditWriter audit)
{
    public async Task<Result<ReviewChangeRequestResponse>> Handle(
        Guid id, ApproveChangeRequestRequest request, CancellationToken ct)
    {
        // Uç noktadaki politika yetmez: handler MCP üzerinden de doğrudan
        // çağrılabiliyor, yetki kontrolü burada da duruyor.
        if (!scope.CanReview)
            return Error.Forbidden("Onay verme yetkiniz yok.");

        var changeRequest = await scope.Apply(db.ChangeRequests)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        // Kapsam dışı istek için 404: 403 isteğin varlığını sızdırır.
        if (changeRequest is null)
            return Error.NotFound("Onay isteği bulunamadı.");

        if (changeRequest.Status != ChangeRequestStatus.Pending)
            return Error.Conflict(
                changeRequest.Status == ChangeRequestStatus.Approved
                    ? "Bu istek zaten onaylanmış."
                    : "Bu istek zaten reddedilmiş.");

        var applied = await applier.ApplyAsync(changeRequest, ct);
        if (!applied.IsSuccess)
            return applied.Error!;

        var change = applied.Value!;
        var reviewedAt = DateTimeOffset.UtcNow;

        changeRequest.Status = ChangeRequestStatus.Approved;
        changeRequest.ReviewedByUserId = currentUser.UserId;
        changeRequest.ReviewedAt = reviewedAt;
        changeRequest.ReviewNote = Clean(request.Note);

        await db.SaveChangesAsync(ct);

        await audit.WriteAsync(
            "ChangeRequest.Approve", nameof(ChangeRequest), changeRequest.Id,
            after: new
            {
                changeRequest.StartupId,
                changeRequest.TargetType,
                changeRequest.Operation,
                change.EntityType,
                change.EntityId,
                changeRequest.ReviewNote
            },
            ct: ct);

        // Veri değişikliği ayrı bir satır olarak da yazılıyor: iz, değişikliğin
        // portaldan mı yoksa doğrudan mı geldiğine bakmadan aynı biçimde
        // sorgulanabilsin ("Startup.Update" her iki yolda da aynı görünür).
        await audit.WriteAsync(
            $"{change.EntityType}.{changeRequest.Operation}",
            change.EntityType, change.EntityId,
            before: change.Before, after: change.After, ct: ct);

        return new ReviewChangeRequestResponse(
            changeRequest.Id,
            changeRequest.Status,
            reviewedAt,
            changeRequest.ReviewNote,
            change.EntityType,
            change.EntityId);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
