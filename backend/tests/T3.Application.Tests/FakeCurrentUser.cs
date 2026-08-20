using T3.Application.Common.Interfaces;
using T3.Domain.Identity;

namespace T3.Application.Tests;

internal sealed class FakeCurrentUser : ICurrentUser
{
    public Guid? UserId { get; init; } = Guid.NewGuid();
    public UserRole? Role { get; init; }
    public Guid? StartupId { get; init; }
    public IReadOnlyCollection<Guid> AssignedProgramIds { get; init; } = [];
    public bool IsAuthenticated { get; init; } = true;

    public static FakeCurrentUser Anonymous => new() { IsAuthenticated = false, UserId = null };

    public static FakeCurrentUser As(UserRole role) => new() { Role = role };
}
