using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Paging;
using T3.Application.Common.Results;
using T3.Application.Features.Users;
using T3.Domain.Registrations;

namespace T3.Application.Features.Registrations.ListRegistrationRequests;

/// <summary>
/// Bekleyen/karara bağlanmış kayıt başvurularının listesi — yalnızca
/// SuperAdmin. Aynı yönetim yüzeyine ait olduğu için tekillik kontrolünün ve
/// yetki kapısının kaynağı da <see cref="UserAdminGuard"/>.
/// </summary>
public sealed class ListRegistrationRequestsHandler(IAppDbContext db, UserAdminGuard guard)
{
    public async Task<Result<PagedResult<RegistrationRequestResponse>>> Handle(
        ListRegistrationRequestsRequest request, CancellationToken ct)
    {
        if (guard.EnsureCanManage() is { } denied)
            return denied;

        var query = db.StartupRegistrationRequests.AsNoTracking().AsQueryable();

        if (request.Status is { } status)
            query = query.Where(r => r.Status == status);

        var total = await query.CountAsync(ct);

        var items = await query
            // Bekleyenler önce: liste SuperAdmin'in iş kuyruğu, karara
            // bağlanmış başvurular geçmiş kaydı.
            .OrderBy(r => r.Status == RegistrationRequestStatus.Pending ? 0 : 1)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new RegistrationRequestResponse(
                r.Id,
                r.Email,
                r.FullName,
                r.StartupName,
                r.Sector,
                r.City,
                r.ContactPhone,
                r.Status,
                r.CreatedAt,
                r.ReviewedAt,
                r.RejectionReason))
            .ToListAsync(ct);

        return new PagedResult<RegistrationRequestResponse>(
            items, request.Page, request.PageSize, total);
    }
}
