using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Domain.Startups;

namespace T3.Application.Features.Startups.Team.RemoveTeamMember;

public sealed class RemoveTeamMemberHandler(
    IAppDbContext db,
    StartupEditGuard guard,
    IAuditWriter audit)
{
    public async Task<Result<bool>> Handle(
        Guid startupId, Guid memberId, CancellationToken ct)
    {
        var access = await guard.ResolveEditableAsync(startupId, ct);
        if (!access.IsSuccess)
            return access.Error!;

        var member = await db.TeamMembers
            .FirstOrDefaultAsync(m => m.Id == memberId && m.StartupId == startupId, ct);

        if (member is null)
            return Error.NotFound("Ekip üyesi bulunamadı.");

        var before = member.ToResponse();

        // Kayıt silinmez, işaretlenir: DeletedAt'i DbContext dolduruyor.
        member.IsDeleted = true;
        await db.SaveChangesAsync(ct);

        await audit.WriteAsync(
            "TeamMember.Delete", nameof(TeamMember), member.Id,
            before: before, ct: ct);

        return true;
    }
}
