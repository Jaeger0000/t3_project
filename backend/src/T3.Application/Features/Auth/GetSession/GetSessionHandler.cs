using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;

namespace T3.Application.Features.Auth.GetSession;

/// <summary>
/// Jetondaki kimliği veritabanıyla tazeler. Jeton 15 dakika geçerli olduğu
/// için ad/rol değişikliği ya da hesabın pasife alınması bu uçta yakalanır;
/// arayüz her açılışta buradan doğrulanır.
/// </summary>
public sealed class GetSessionHandler(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<Result<SessionUserResponse>> Handle(CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            return Error.Forbidden("Oturum bulunamadı.");

        var user = await db.Users
            .Include(u => u.Startup)
            .Include(u => u.ProgramAssignments)
                .ThenInclude(a => a.Program)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null || !user.IsActive)
            return Error.Forbidden("Hesabınız aktif değil.");

        var programs = user.ProgramAssignments
            .Select(a => new AssignedProgramResponse(a.ProgramId, a.Program.Name))
            .OrderBy(p => p.Name)
            .ToArray();

        return SessionUserMapper.Map(user, programs);
    }
}
