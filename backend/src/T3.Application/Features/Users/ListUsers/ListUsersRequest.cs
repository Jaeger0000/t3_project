using T3.Application.Common.Paging;
using T3.Domain.Identity;

namespace T3.Application.Features.Users.ListUsers;

public sealed record ListUsersRequest : PagedRequest
{
    /// <summary>Ad ve e-postada geçen serbest metin araması.</summary>
    public string? Q { get; init; }

    public UserRole? Role { get; init; }

    /// <summary>Boş bırakılırsa pasif hesaplar da listelenir.</summary>
    public bool? IsActive { get; init; }

    public Guid? ProgramId { get; init; }
    public Guid? StartupId { get; init; }
}
