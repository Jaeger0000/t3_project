using T3.Domain.Identity;

namespace T3.Application.Common.Interfaces;

/// <summary>Denetim izi yazıcısı — her yazma işleminden sonra çağrılır.</summary>
public interface IAuditWriter
{
    Task WriteAsync(
        string action,
        string entityType,
        Guid? entityId,
        object? before = null,
        object? after = null,
        CancellationToken ct = default);

    /// <summary>
    /// Aktörü açıkça verilen kayıt. Giriş olayları için gerekli: jeton henüz
    /// üretilmediğinden <see cref="ICurrentUser"/> boştur, kimin denediğini
    /// yalnızca handler bilir. Başarısız denemede aktör hiç bilinmiyorsa
    /// <c>null</c> geçilir — iz "kimlik doğrulanmadı" olarak okunur.
    /// </summary>
    Task WriteForActorAsync(
        Guid? actorUserId,
        UserRole? actorRole,
        string action,
        string entityType,
        Guid? entityId,
        object? before = null,
        object? after = null,
        CancellationToken ct = default);
}
