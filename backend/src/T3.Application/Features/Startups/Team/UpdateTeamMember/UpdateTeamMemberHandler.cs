using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Domain.Startups;

namespace T3.Application.Features.Startups.Team.UpdateTeamMember;

public sealed class UpdateTeamMemberHandler(
    IAppDbContext db,
    StartupEditGuard guard,
    IAuditWriter audit)
{
    public async Task<Result<TeamMemberResponse>> Handle(
        Guid startupId, Guid memberId, TeamMemberWriteModel model, CancellationToken ct)
    {
        var access = await guard.ResolveEditableAsync(startupId, ct);
        if (!access.IsSuccess)
            return access.Error!;

        // StartupId koşulu kasıtlı: başka girişimin üyesini rotadaki kimlikle
        // düzenlemeye çalışan istek burada durur.
        var member = await db.TeamMembers
            .FirstOrDefaultAsync(m => m.Id == memberId && m.StartupId == startupId, ct);

        if (member is null)
            return Error.NotFound("Ekip üyesi bulunamadı.");

        var before = member.ToResponse();
        model.ApplyTo(member);
        await db.SaveChangesAsync(ct);

        await audit.WriteAsync(
            "TeamMember.Update", nameof(TeamMember), member.Id,
            before: before, after: member.ToResponse(), ct: ct);

        return member.ToResponse();
    }
}
