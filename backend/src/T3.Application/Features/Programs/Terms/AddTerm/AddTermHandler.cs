using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Application.Common.Text;
using T3.Domain.Programs;

namespace T3.Application.Features.Programs.Terms.AddTerm;

/// <summary>
/// Programa dönem ekler.
///
/// Program tanımının aksine bu işlem Program Yöneticisi'ne de açık: dönem açmak
/// günlük operasyon, yetki kapsamını büyütmüyor. Kapsam kontrolü
/// <see cref="ProgramAccessGuard"/> üzerinden — kendi programı dışında dönem
/// açamaz.
/// </summary>
public sealed class AddTermHandler(
    IAppDbContext db,
    ProgramAccessGuard guard,
    IAuditWriter audit)
{
    public async Task<Result<ProgramTermSummaryResponse>> Handle(
        Guid programId, ProgramTermWriteModel model, CancellationToken ct)
    {
        if (await guard.EnsureOwnsProgramAsync(programId, ct) is { } denied)
            return denied;

        var program = await db.Programs
            .Where(p => p.Id == programId)
            .Select(p => new { p.Id, p.Name })
            .FirstAsync(ct);

        var name = model.Name.Trim();
        var normalized = SearchText.Normalize(name);

        var taken = await db.ProgramTerms
            .IgnoreQueryFilters()
            .AnyAsync(t => t.ProgramId == programId && t.Name.ToLower() == normalized, ct);

        if (taken)
            return Error.Conflict($"\"{program.Name}\" programında \"{name}\" dönemi zaten var.");

        var term = new ProgramTerm { ProgramId = programId };
        model.ApplyTo(term);

        db.ProgramTerms.Add(term);
        await db.SaveChangesAsync(ct);

        var response = new ProgramTermSummaryResponse(
            term.Id, program.Id, program.Name, term.Name, term.StartsOn, term.EndsOn, 0);

        await audit.WriteAsync(
            "ProgramTerm.Create", nameof(ProgramTerm), term.Id,
            after: response, ct: ct);

        return response;
    }
}
