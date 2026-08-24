using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Domain.Identity;

namespace T3.Application.Features.Users.UpdateUser;

public sealed class UpdateUserHandler(
    IAppDbContext db,
    UserAdminGuard guard,
    IAuditWriter audit)
{
    public async Task<Result<UserResponse>> Handle(
        Guid id, UpdateUserRequest request, CancellationToken ct)
    {
        if (guard.EnsureCanManage() is { } denied)
            return denied;

        var user = await db.Users
            .Include(u => u.ProgramAssignments)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user is null)
            return Error.NotFound("Kullanıcı bulunamadı.");

        // Kendini kilitleme koruması: rol değişikliği ya da pasife alma kendi
        // hesabında yapılamaz. Yalnızca değişiklik varsa engelliyoruz, adını
        // güncellemek serbest.
        var locksSelfOut = request.Role != user.Role || (!request.IsActive && user.IsActive);

        if (locksSelfOut && guard.EnsureNotSelf(id, "rol veya durum değişikliği") is { } self)
            return self;

        if (UserAdminGuard.ValidateBinding(request.Role, request.StartupId, request.ProgramIds)
            is { } binding)
            return binding;

        if (await guard.EnsureStartupExistsAsync(request.StartupId, ct) is { } noStartup)
            return noStartup;

        var before = new
        {
            user.FullName,
            user.Role,
            user.StartupId,
            ProgramIds = user.ProgramAssignments
                .Where(a => !a.IsDeleted).Select(a => a.ProgramId).ToArray(),
            user.IsActive
        };

        user.FullName = request.FullName.Trim();
        user.Role = request.Role;
        user.StartupId = request.StartupId;
        user.IsActive = request.IsActive;

        if (await guard.SyncProgramsAsync(user, request.ProgramIds, ct) is { } noProgram)
            return noProgram;

        await db.SaveChangesAsync(ct);

        await audit.WriteAsync(
            "User.Update", nameof(User), user.Id,
            before: before,
            after: new
            {
                user.FullName,
                user.Role,
                user.StartupId,
                ProgramIds = request.ProgramIds ?? [],
                user.IsActive
            },
            ct: ct);

        return await guard.LoadResponseAsync(user.Id, ct);
    }
}
