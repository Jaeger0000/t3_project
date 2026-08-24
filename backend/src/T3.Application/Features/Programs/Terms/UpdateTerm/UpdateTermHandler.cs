using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Application.Common.Text;
using T3.Domain.Programs;

namespace T3.Application.Features.Programs.Terms.UpdateTerm;

public sealed class UpdateTermHandler(
    IAppDbContext db,
    ProgramAccessGuard guard,
    IAuditWriter audit)
{
    public async Task<Result<ProgramTermSummaryResponse>> Handle(
        Guid programId, Guid termId, ProgramTermWriteModel model, CancellationToken ct)
    {
        if (await guard.EnsureOwnsProgramAsync(programId, ct) is { } denied)
            return denied;

        // Dönem, adres çubuğundaki programa gerçekten ait olmalı: aksi hâlde
        // kendi programının kimliğiyle başka programın dönemini düzenlemek
        // mümkün olurdu (IDOR).
        var term = await db.ProgramTerms
            .Include(t => t.Program)
            .FirstOrDefaultAsync(t => t.Id == termId && t.ProgramId == programId, ct);

        if (term is null)
            return Error.NotFound("Program dönemi bulunamadı.");

        var name = model.Name.Trim();
        var normalized = SearchText.Normalize(name);

        var taken = await db.ProgramTerms
            .IgnoreQueryFilters()
            .AnyAsync(t => t.ProgramId == programId
                           && t.Id != termId
                           && t.Name.ToLower() == normalized, ct);

        if (taken)
            return Error.Conflict($"\"{term.Program.Name}\" programında \"{name}\" dönemi zaten var.");

        var before = new { term.Name, term.StartsOn, term.EndsOn };

        model.ApplyTo(term);
        await db.SaveChangesAsync(ct);

        var participantCount = await db.ProgramParticipations
            .CountAsync(p => p.ProgramTermId == termId, ct);

        var response = new ProgramTermSummaryResponse(
            term.Id, term.ProgramId, term.Program.Name,
            term.Name, term.StartsOn, term.EndsOn, participantCount);

        await audit.WriteAsync(
            "ProgramTerm.Update", nameof(ProgramTerm), term.Id,
            before: before, after: response, ct: ct);

        return response;
    }
}
