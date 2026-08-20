using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Domain.Identity;
using T3.Domain.Programs;

namespace T3.Application.Features.Programs.ListPrograms;

public sealed record ProgramTermResponse(
    Guid Id,
    string Name,
    DateOnly StartsOn,
    DateOnly? EndsOn,
    int ParticipantCount);

public sealed record ProgramResponse(
    Guid Id,
    string Name,
    ProgramType Type,
    string? Coordinatorship,
    string? Description,
    int StartupCount,
    IReadOnlyList<ProgramTermResponse> Terms);

/// <summary>
/// Program listesi — filtre açılırlarını ve katılım formlarını besler.
/// Kapsam girişim listesindekiyle aynı mantığı izler: Program Yöneticisi
/// yalnızca atandığı programları, girişim kullanıcısı yalnızca geçtiği
/// programları görür.
/// </summary>
public sealed class ListProgramsHandler(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<Result<IReadOnlyList<ProgramResponse>>> Handle(CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Error.Forbidden("Oturum bulunamadı.");

        var query = Scope(db.Programs.AsNoTracking());

        var programs = await query
            .OrderBy(p => p.Name)
            .Select(p => new ProgramResponse(
                p.Id,
                p.Name,
                p.Type,
                p.Coordinatorship,
                p.Description,
                p.Terms.SelectMany(t => t.Participations)
                    .Select(pt => pt.StartupId)
                    .Distinct()
                    .Count(),
                p.Terms
                    .OrderByDescending(t => t.StartsOn)
                    .Select(t => new ProgramTermResponse(
                        t.Id,
                        t.Name,
                        t.StartsOn,
                        t.EndsOn,
                        t.Participations.Count))
                    .ToList()))
            .ToListAsync(ct);

        return programs;
    }

    private IQueryable<EcosystemProgram> Scope(IQueryable<EcosystemProgram> query)
    {
        switch (currentUser.Role)
        {
            case UserRole.SuperAdmin:
            case UserRole.DecisionMaker:
                return query;

            case UserRole.ProgramManager:
                var assigned = currentUser.AssignedProgramIds.ToArray();
                return assigned.Length == 0
                    ? query.Where(_ => false)
                    : query.Where(p => assigned.Contains(p.Id));

            case UserRole.StartupUser:
                var startupId = currentUser.StartupId;
                return startupId is null
                    ? query.Where(_ => false)
                    : query.Where(p => p.Terms
                        .Any(t => t.Participations.Any(pt => pt.StartupId == startupId)));

            default:
                return query.Where(_ => false);
        }
    }
}
