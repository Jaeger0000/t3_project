using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Application.Common.Text;
using T3.Domain.Programs;

namespace T3.Application.Features.Programs.UpdateProgram;

public sealed class UpdateProgramHandler(
    IAppDbContext db,
    ProgramAccessGuard guard,
    IAuditWriter audit)
{
    public async Task<Result<ProgramSummaryResponse>> Handle(
        Guid id, ProgramWriteModel model, CancellationToken ct)
    {
        if (guard.EnsureCanManagePrograms() is { } denied)
            return denied;

        var program = await db.Programs.FirstOrDefaultAsync(p => p.Id == id, ct);

        if (program is null)
            return Error.NotFound("Program bulunamadı.");

        var name = model.Name.Trim();
        var normalized = SearchText.Normalize(name);

        var taken = await db.Programs
            .IgnoreQueryFilters()
            .AnyAsync(p => p.Id != id && p.Name.ToLower() == normalized, ct);

        if (taken)
            return Error.Conflict($"\"{name}\" adlı başka bir program zaten kayıtlı.");

        var before = new ProgramSummaryResponse(
            program.Id, program.Name, program.Type,
            program.Coordinatorship, program.Description);

        model.ApplyTo(program);
        await db.SaveChangesAsync(ct);

        var after = new ProgramSummaryResponse(
            program.Id, program.Name, program.Type,
            program.Coordinatorship, program.Description);

        await audit.WriteAsync(
            "Program.Update", nameof(EcosystemProgram), program.Id,
            before: before, after: after, ct: ct);

        return after;
    }
}
