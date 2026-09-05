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
        CancellationToken ct = default,
        bool saveChanges = true);

    /// <summary>
    /// Aktörü açıkça verilen kayıt. Giriş olayları için gerekli: jeton henüz
    /// üretilmediğinden <see cref="ICurrentUser"/> boştur, kimin denediğini
    /// yalnızca handler bilir. Başarısız denemede aktör hiç bilinmiyorsa
    /// <c>null</c> geçilir — iz "kimlik doğrulanmadı" olarak okunur.
    /// </summary>
    /// <param name="saveChanges">
    /// <c>false</c> ise satır yalnızca izlenen bağlama eklenir, kalıcı hâle
    /// getirilmez — çağıran taraf kendi varlık değişikliğiyle <b>birlikte</b>,
    /// tek bir <c>SaveChangesAsync</c> ile yazmalı. Aksi hâlde iş değişikliği
    /// kalıcı olur ama süreç iz satırından önce düşerse (ya da tersi) veri ile
    /// iz birbirinden kopar (bkz. G-09, Guvenlik_Denetimi_ve_Iyilestirme_Plani.md).
    /// Varsayılan <c>true</c>: mevcut çağıranların davranışı değişmez.
    /// </param>
    Task WriteForActorAsync(
        Guid? actorUserId,
        UserRole? actorRole,
        string action,
        string entityType,
        Guid? entityId,
        object? before = null,
        object? after = null,
        CancellationToken ct = default,
        bool saveChanges = true);
}
