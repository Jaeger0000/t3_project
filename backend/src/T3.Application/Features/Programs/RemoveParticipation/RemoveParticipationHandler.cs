using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Domain.Programs;

namespace T3.Application.Features.Programs.RemoveParticipation;

public sealed record RemoveParticipationResponse(
    Guid Id,
    Guid StartupId,
    string StartupName,
    string ProgramName,
    string TermName);

/// <summary>
/// Katılımı pasife alır — yanlış girişime eklenen kayıt gelişim yolculuğundan
/// düşsün. Kayıt silinmiyor, işaretleniyor: "bir dönem bu programa kayıtlıydı,
/// sonra kaldırıldı" bilgisi denetim izinde durmalı.
/// </summary>
public sealed class RemoveParticipationHandler(
    IAppDbContext db,
    ProgramAccessGuard guard,
    IAuditWriter audit)
{
    public async Task<Result<RemoveParticipationResponse>> Handle(
        Guid id, CancellationToken ct)
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

        participation.IsDeleted = true;
        await db.SaveChangesAsync(ct);

        var response = new RemoveParticipationResponse(
            participation.Id,
            participation.StartupId,
            participation.Startup.Name,
            participation.ProgramTerm.Program.Name,
            participation.ProgramTerm.Name);

        await audit.WriteAsync(
            "ProgramParticipation.Delete", nameof(ProgramParticipation), participation.Id,
            before: new
            {
                participation.Status,
                participation.JoinedOn,
                participation.LeftOn
            },
            after: response,
            ct: ct);

        return response;
    }
}
