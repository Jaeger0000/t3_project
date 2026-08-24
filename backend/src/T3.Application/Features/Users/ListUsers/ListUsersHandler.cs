using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Paging;
using T3.Application.Common.Results;
using T3.Application.Common.Text;

namespace T3.Application.Features.Users.ListUsers;

/// <summary>
/// Kullanıcı listesi — yalnızca SuperAdmin. Kullanıcı kayıtları e-posta ve rol
/// bağı taşıdığı için ekosistemin yetki haritası; başka rollere açılmıyor.
/// </summary>
public sealed class ListUsersHandler(IAppDbContext db, UserAdminGuard guard)
{
    public async Task<Result<PagedResult<UserResponse>>> Handle(
        ListUsersRequest request, CancellationToken ct)
    {
        if (guard.EnsureCanManage() is { } denied)
            return denied;

        var query = db.Users
            .AsNoTracking()
            .Include(u => u.Startup)
            .Include(u => u.ProgramAssignments)
                .ThenInclude(a => a.Program)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            // Türkçe noktalı İ için normalleştirme zorunlu; bkz. SearchText.
            var term = SearchText.Normalize(request.Q);
            query = query.Where(u =>
                u.FullName.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
        }

        if (request.Role is { } role)
            query = query.Where(u => u.Role == role);

        if (request.IsActive is { } isActive)
            query = query.Where(u => u.IsActive == isActive);

        if (request.StartupId is { } startupId)
            query = query.Where(u => u.StartupId == startupId);

        if (request.ProgramId is { } programId)
            query = query.Where(u => u.ProgramAssignments.Any(a => a.ProgramId == programId));

        var total = await query.CountAsync(ct);

        var users = await query
            // Pasif hesaplar listenin sonuna: liste bir yönetim ekranı,
            // kullanımda olan hesaplar önce gelmeli.
            .OrderByDescending(u => u.IsActive)
            .ThenBy(u => u.Role)
            .ThenBy(u => u.FullName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResult<UserResponse>(
            [.. users.Select(u => u.ToResponse())], request.Page, request.PageSize, total);
    }
}
