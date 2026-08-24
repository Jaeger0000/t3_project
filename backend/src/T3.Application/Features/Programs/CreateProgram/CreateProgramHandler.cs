using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Application.Common.Text;
using T3.Domain.Programs;

namespace T3.Application.Features.Programs.CreateProgram;

/// <summary>
/// Yeni program tanımlar. Bu uçtan önce program yalnızca tohumlayıcıyla, yani
/// doğrudan veritabanına yazılarak var olabiliyordu — zincirin
/// (program → dönem → katılım) ilk halkasının arayüz yolu yoktu.
/// </summary>
public sealed class CreateProgramHandler(
    IAppDbContext db,
    ProgramAccessGuard guard,
    IAuditWriter audit)
{
    public async Task<Result<ProgramSummaryResponse>> Handle(
        ProgramWriteModel model, CancellationToken ct)
    {
        if (guard.EnsureCanManagePrograms() is { } denied)
            return denied;

        var name = model.Name.Trim();
        var normalized = SearchText.Normalize(name);

        // Silinmiş kayıtlar da sayılıyor: tekillik kısıtı veritabanında
        // soft-delete'ten bağımsız, aksi hâlde doğrulamayı geçen kayıt insert
        // sırasında patlardı.
        var taken = await db.Programs
            .IgnoreQueryFilters()
            .AnyAsync(p => p.Name.ToLower() == normalized, ct);

        if (taken)
            return Error.Conflict($"\"{name}\" adlı bir program zaten kayıtlı.");

        var program = new EcosystemProgram();
        model.ApplyTo(program);

        db.Programs.Add(program);
        await db.SaveChangesAsync(ct);

        var response = new ProgramSummaryResponse(
            program.Id, program.Name, program.Type,
            program.Coordinatorship, program.Description);

        await audit.WriteAsync(
            "Program.Create", nameof(EcosystemProgram), program.Id,
            after: response, ct: ct);

        return response;
    }
}
