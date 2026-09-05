using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using T3.Application.Common.Interfaces;

namespace T3.Infrastructure.Identity;

/// <summary>
/// <see cref="IUserStateProvider"/>'ın DB destekli uygulaması. Önbellek süresi
/// kısa tutuluyor (45 sn) ve handler'lar durumu değiştirdiklerinde
/// <see cref="Invalidate"/> ile hemen boşaltıyor — "yetkiyi geri alma" bu
/// yüzden pratikte anlık, yalnızca önbelleği olmayan başka bir kullanıcının
/// isteği ilk 45 sn içinde gelirse gecikir.
/// </summary>
public sealed class UserStateProvider(IAppDbContext db, IMemoryCache cache) : IUserStateProvider
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(45);

    private static string KeyFor(Guid userId) => $"userstate:{userId}";

    public async Task<UserState?> GetAsync(Guid userId, CancellationToken ct = default)
    {
        if (cache.TryGetValue(KeyFor(userId), out UserState? cached))
            return cached;

        var state = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserState(u.IsActive, u.Role, u.SecurityStamp, u.MustChangePassword))
            .FirstOrDefaultAsync(ct);

        cache.Set(KeyFor(userId), state, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration,
            Size = 1,
        });

        return state;
    }

    public void Invalidate(Guid userId) => cache.Remove(KeyFor(userId));
}
