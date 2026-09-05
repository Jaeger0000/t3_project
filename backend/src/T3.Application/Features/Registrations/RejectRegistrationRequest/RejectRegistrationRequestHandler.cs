using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Application.Features.Users;
using T3.Domain.Registrations;

namespace T3.Application.Features.Registrations.RejectRegistrationRequest;

/// <summary>
/// Ret: hiçbir Startup/User satırı doğmaz, yalnızca başvuru kapanır. Kayıt
/// silinmiyor — reddedilen başvurular da denetim izinin parçası.
/// </summary>
public sealed class RejectRegistrationRequestHandler(
    IAppDbContext db,
    UserAdminGuard guard,
    ICurrentUser currentUser,
    IAuditWriter audit)
{
    public async Task<Result<bool>> Handle(
        Guid id, RejectRegistrationRequestRequest request, CancellationToken ct)
    {
        if (guard.EnsureCanManage() is { } denied)
            return denied;

        var registration = await db.StartupRegistrationRequests
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (registration is null)
            return Error.NotFound("Başvuru bulunamadı.");

        if (registration.Status != RegistrationRequestStatus.Pending)
            return Error.Conflict("Bu başvuru zaten sonuçlandırılmış.");

        registration.Status = RegistrationRequestStatus.Rejected;
        registration.RejectionReason = request.Note.Trim();
        registration.ReviewedAt = DateTimeOffset.UtcNow;
        registration.ReviewedByUserId = currentUser.UserId;

        await db.SaveChangesAsync(ct);

        await audit.WriteAsync(
            "StartupRegistration.Reject", nameof(StartupRegistrationRequest), registration.Id,
            after: new { registration.Email, registration.StartupName, registration.RejectionReason },
            ct: ct);

        return true;
    }
}
