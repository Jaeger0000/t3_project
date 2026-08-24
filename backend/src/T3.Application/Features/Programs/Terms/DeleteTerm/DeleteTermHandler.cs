using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Domain.Programs;

namespace T3.Application.Features.Programs.Terms.DeleteTerm;

public sealed record DeleteTermResponse(Guid Id, string Name);

/// <summary>
/// Dönemi pasife alır.
///
/// Katılım varsa <b>reddediyor</b>, zinciri sessizce silmiyor: katılım kayıtları
/// girişimlerin gelişim yolculuğunun kaynağı ve bu uç Program Yöneticisi'ne de
/// açık. Tek tıkla başkasının profil geçmişini boşaltabilen bir işlem olmamalı;
/// katılımlar tek tek kaldırılınca dönem silinebilir hâle geliyor. (Programın
/// tamamını kapatmak farklı bir karar ve yalnızca sistem yöneticisinde.)
/// </summary>
public sealed class DeleteTermHandler(
    IAppDbContext db,
    ProgramAccessGuard guard,
    IAuditWriter audit)
{
    public async Task<Result<DeleteTermResponse>> Handle(
        Guid programId, Guid termId, CancellationToken ct)
    {
        if (await guard.EnsureOwnsProgramAsync(programId, ct) is { } denied)
            return denied;

        var term = await db.ProgramTerms
            .FirstOrDefaultAsync(t => t.Id == termId && t.ProgramId == programId, ct);

        if (term is null)
            return Error.NotFound("Program dönemi bulunamadı.");

        var participants = await db.ProgramParticipations
            .CountAsync(p => p.ProgramTermId == termId, ct);

        if (participants > 0)
            return Error.Conflict(
                $"\"{term.Name}\" döneminde {participants} girişim kayıtlı. " +
                "Dönemi kapatmak için önce katılımları kaldırın.");

        term.IsDeleted = true;
        await db.SaveChangesAsync(ct);

        await audit.WriteAsync(
            "ProgramTerm.Delete", nameof(ProgramTerm), term.Id,
            before: new { term.Name, term.StartsOn, term.EndsOn },
            ct: ct);

        return new DeleteTermResponse(term.Id, term.Name);
    }
}
