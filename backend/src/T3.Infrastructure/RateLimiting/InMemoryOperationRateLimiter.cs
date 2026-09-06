using Microsoft.Extensions.Caching.Memory;
using T3.Application.Common.Interfaces;

namespace T3.Infrastructure.RateLimiting;

/// <summary>
/// <see cref="IOperationRateLimiter"/>'ın bellek içi uygulaması. Sabit
/// pencere sayacı: HTTP ara katmanındaki <c>AuthRateLimit.PartitionMassExport</c>
/// ile aynı mantık (bkz. o dosyadaki gerekçe), yalnızca endpoint dışında,
/// tek bir uç içindeki farklı maliyetli işlemler için kullanılabilir hâlde.
/// </summary>
public sealed class InMemoryOperationRateLimiter(IMemoryCache cache) : IOperationRateLimiter
{
    public bool TryConsume(string key, Guid userId, int limit, TimeSpan window)
    {
        var cacheKey = $"op-rate|{key}|{userId}";
        var count = cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = window;
            entry.Size = 1;
            return new Counter();
        })!;

        lock (count)
        {
            if (count.Value >= limit)
                return false;

            count.Value++;
            return true;
        }
    }

    private sealed class Counter
    {
        public int Value;
    }
}
