using Microsoft.Extensions.Caching.Memory;
using T3.Application.Common.Interfaces;

namespace T3.Infrastructure.Assistant;

/// <summary>
/// <see cref="IAssistantExportStore"/>'ın bellek içi uygulaması. Kalıcı depoya
/// gerek yok: dosya tek bir sohbet turunun ömrü kadar yaşıyor, sunucu yeniden
/// başlarsa jetonun geçersizleşmesi kabul edilebilir (kullanıcı aracı tekrar
/// çağırır).
/// </summary>
public sealed class InMemoryAssistantExportStore(IMemoryCache cache) : IAssistantExportStore
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

    public string Save(Guid ownerUserId, AssistantExportFile file)
    {
        var token = Guid.NewGuid().ToString("N");

        cache.Set(KeyFor(token), (ownerUserId, file), new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = Lifetime,
            Size = 1,
        });

        return token;
    }

    public AssistantExportFile? Take(Guid ownerUserId, string token)
    {
        if (!cache.TryGetValue(KeyFor(token), out var entry)
            || entry is not (Guid owner, AssistantExportFile file)
            || owner != ownerUserId)
            return null;

        // Tek kullanımlık: aynı jeton ikinci kez denenirse (paylaşılmış ya da
        // tarayıcı geri tuşuyla tekrarlanmış istek) "bulunamadı" dönsün.
        cache.Remove(KeyFor(token));
        return file;
    }

    private static string KeyFor(string token) => $"assistant-export:{token}";
}
