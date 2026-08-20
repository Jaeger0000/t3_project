using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Domain.Startups;

namespace T3.Application.Features.Startups.Team.AddTeamMember;

public sealed class AddTeamMemberHandler(
    IAppDbContext db,
    StartupEditGuard guard,
    IAuditWriter audit)
{
    public async Task<Result<TeamMemberResponse>> Handle(
        Guid startupId, TeamMemberWriteModel model, CancellationToken ct)
    {
        var access = await guard.ResolveEditableAsync(startupId, ct);
        if (!access.IsSuccess)
            return access.Error!;

        var member = new TeamMember { StartupId = startupId };
        model.ApplyTo(member);

        db.TeamMembers.Add(member);
        await db.SaveChangesAsync(ct);

        await audit.WriteAsync(
            "TeamMember.Create", nameof(TeamMember), member.Id,
            after: member.ToResponse(), ct: ct);

        return member.ToResponse();
    }
}
