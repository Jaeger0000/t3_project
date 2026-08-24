using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Domain.Programs;

namespace T3.Application.Features.Programs.UpdateParticipation;

/// <summary>
/// Yanlış girilen katılımı düzeltir (durum, tarih, not).
///
/// Yetki <see cref="Common.Rbac.IStartupScope"/> değil <em>program sahipliği</em>
/// üzerinden: kayıt bir programın dönemine ait ve o dönemin sorumlusu kim ise
/// düzeltme onun işi. Bu, AddParticipation'daki gerekçenin aynısı.
/// </summary>
public sealed class UpdateParticipationHandler(
    IAppDbContext db,
    ProgramAccessGuard guard,
    IAuditWriter audit)
{
    public async Task<Result<UpdateParticipationResponse>> Handle(
        Guid id, UpdateParticipationRequest request, CancellationToken ct)
    {
        var participation = await db.ProgramParticipations
            .Include(p => p.Startup)
            .Include(p => p.ProgramTerm)
                .ThenInclude(t => t.Program)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (participation is null)
            return Error.NotFound("Katılım kaydı bulunamadı.");

        if (await guard.EnsureOwnsProgramAsync(participation.ProgramTerm.ProgramId, ct)
            is { } denied)
            return denied;

        var before = new
        {
            participation.Status,
            participation.JoinedOn,
            participation.LeftOn,
            participation.Notes
        };

        participation.Status = request.Status;
        participation.JoinedOn = request.JoinedOn;
        participation.LeftOn = request.LeftOn;
        participation.Notes = string.IsNullOrWhiteSpace(request.Notes)
            ? null
            : request.Notes.Trim();

        await db.SaveChangesAsync(ct);

        var response = new UpdateParticipationResponse(
            participation.Id,
            participation.StartupId,
            participation.Startup.Name,
            participation.ProgramTerm.ProgramId,
            participation.ProgramTerm.Program.Name,
            participation.ProgramTerm.Name,
            participation.Status,
            participation.JoinedOn,
            participation.LeftOn,
            participation.Notes);

        await audit.WriteAsync(
            "ProgramParticipation.Update", nameof(ProgramParticipation), participation.Id,
            before: before, after: response, ct: ct);

        return response;
    }
}
