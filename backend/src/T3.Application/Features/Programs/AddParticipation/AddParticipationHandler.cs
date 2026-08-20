using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Domain.Identity;
using T3.Domain.Programs;

namespace T3.Application.Features.Programs.AddParticipation;

/// <summary>
/// Girişimi bir program dönemine bağlar — gelişim yolculuğunun (MVP #2)
/// birincil kaynağı.
///
/// Yetki bilinçli olarak <see cref="Common.Rbac.IStartupScope"/> üzerinden
/// değil <em>program sahipliği</em> üzerinden kontrol edilir. Nedeni yapısal:
/// Program Yöneticisi'nin kapsamı "programlarımdan geçmiş girişimler" olarak
/// tanımlı, dolayısıyla girişim kapsamı şart koşulsa yeni bir girişim o
/// kapsama hiçbir zaman giremezdi. Bu uç, kapsamın kendisini kuran uçtur.
/// </summary>
public sealed class AddParticipationHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IAuditWriter audit)
{
    public async Task<Result<AddParticipationResponse>> Handle(
        AddParticipationRequest request, CancellationToken ct)
    {
        if (currentUser.Role is not (UserRole.SuperAdmin or UserRole.ProgramManager))
            return Error.Forbidden("Program katılımı ekleme yetkiniz yok.");

        var term = await db.ProgramTerms.AsNoTracking()
            .Where(t => t.Id == request.ProgramTermId)
            .Select(t => new
            {
                t.Id,
                TermName = t.Name,
                t.ProgramId,
                ProgramName = t.Program.Name,
                t.StartsOn
            })
            .FirstOrDefaultAsync(ct);

        if (term is null)
            return Error.NotFound("Program dönemi bulunamadı.");

        if (currentUser.Role is UserRole.ProgramManager
            && !currentUser.AssignedProgramIds.Contains(term.ProgramId))
            return Error.Forbidden("Bu program sizin sorumluluğunuzda değil.");

        // Girişim varlığı kapsam filtresi olmadan doğrulanır; işlemin yetkisi
        // program sahipliğinden gelir. Guid tahmin edilemez olduğu için bu
        // kontrol bir numaralandırma yüzeyi oluşturmuyor.
        var startup = await db.Startups.AsNoTracking()
            .Where(s => s.Id == request.StartupId)
            .Select(s => new { s.Id, s.Name })
            .FirstOrDefaultAsync(ct);

        if (startup is null)
            return Error.NotFound("Girişim bulunamadı.");

        var alreadyJoined = await db.ProgramParticipations
            .AnyAsync(p => p.StartupId == request.StartupId
                           && p.ProgramTermId == request.ProgramTermId, ct);

        if (alreadyJoined)
            return Error.Conflict($"{startup.Name} bu döneme zaten kayıtlı.");

        var participation = new ProgramParticipation
        {
            StartupId = request.StartupId,
            ProgramTermId = request.ProgramTermId,
            Status = request.Status,
            JoinedOn = request.JoinedOn,
            LeftOn = request.LeftOn,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        };

        db.ProgramParticipations.Add(participation);
        await db.SaveChangesAsync(ct);

        var response = new AddParticipationResponse(
            participation.Id,
            startup.Id,
            startup.Name,
            term.ProgramId,
            term.ProgramName,
            term.TermName,
            participation.Status,
            participation.JoinedOn,
            participation.LeftOn);

        await audit.WriteAsync(
            "ProgramParticipation.Create", nameof(ProgramParticipation),
            participation.Id, after: response, ct: ct);

        return response;
    }
}
