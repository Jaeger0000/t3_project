using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Domain.Common;
using T3.Domain.Programs;

namespace T3.Application.Features.Programs.DeleteProgram;

/// <summary>
/// Silme sonucunun özeti: kaç dönem, kaç katılım ve kaç yönetici ataması
/// kapandı. Girişim silmede olduğu gibi yönetici ne kadarını kapattığını
/// görmeden onaylamamalı — program kapatmak yetki kapsamını da daraltıyor.
/// </summary>
public sealed record DeleteProgramResponse(
    Guid Id,
    string Name,
    int Terms,
    int Participations,
    int ManagerAssignments);

/// <summary>
/// Programı ve zincirini pasife alır.
///
/// Global soft-delete süzgeci yalnızca <em>okumayı</em> daraltır: program
/// silindi diye dönemleri ve katılımları kendiliğinden gizlenmez. Zincir bu
/// yüzden elle yürüyor — aksi hâlde program listeden kalkar ama girişimlerin
/// gelişim yolculuğunda "hayalet dönem" görünmeye devam ederdi.
/// </summary>
public sealed class DeleteProgramHandler(
    IAppDbContext db,
    ProgramAccessGuard guard,
    IAuditWriter audit)
{
    public async Task<Result<DeleteProgramResponse>> Handle(Guid id, CancellationToken ct)
    {
        if (guard.EnsureCanManagePrograms() is { } denied)
            return denied;

        var program = await db.Programs.FirstOrDefaultAsync(p => p.Id == id, ct);

        if (program is null)
            return Error.NotFound("Program bulunamadı.");

        var termIds = await db.ProgramTerms
            .Where(t => t.ProgramId == id)
            .Select(t => t.Id)
            .ToListAsync(ct);

        var participations = await MarkAsync(
            db.ProgramParticipations.Where(p => termIds.Contains(p.ProgramTermId)), ct);

        var terms = await MarkAsync(db.ProgramTerms.Where(t => t.ProgramId == id), ct);

        // Yönetici atamaları da kapanıyor: kapatılmış bir programa yetkili
        // kalmak kapsamı belirsizleştirir. Kullanıcı kaydı etkilenmiyor.
        var assignments = await MarkAsync(
            db.UserProgramAssignments.Where(a => a.ProgramId == id), ct);

        program.IsDeleted = true;
        await db.SaveChangesAsync(ct);

        var response = new DeleteProgramResponse(
            program.Id, program.Name, terms, participations, assignments);

        await audit.WriteAsync(
            "Program.Delete", nameof(EcosystemProgram), program.Id,
            before: new { program.Name, program.Type },
            after: response,
            ct: ct);

        return response;
    }

    /// <summary>
    /// Bağlı satırları işaretler. Toplu güncelleme (<c>ExecuteUpdate</c>)
    /// kullanılmıyor: o API sağlayıcıya özgü ve Application katmanı sağlayıcı
    /// bilmiyor.
    /// </summary>
    private async Task<int> MarkAsync<T>(IQueryable<T> query, CancellationToken ct)
        where T : class, ISoftDelete
    {
        var rows = await query.ToListAsync(ct);

        foreach (var row in rows)
            row.IsDeleted = true;

        return rows.Count;
    }
}
