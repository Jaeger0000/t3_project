using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Domain.Identity;
using T3.Domain.Startups;

namespace T3.Application.Features.Startups.DeleteStartup;

/// <summary>
/// Silme sonucunun özeti. Kaç bağlı kaydın işaretlendiği yanıtta dönüyor:
/// silme geri alınması zor bir işlem, yönetici ne kadarını kapattığını
/// görmeden onaylamamalı.
/// </summary>
public sealed record DeleteStartupResponse(
    Guid Id,
    string Name,
    int TeamMembers,
    int Participations,
    int Milestones,
    int Achievements,
    int Documents,
    int ChangeRequests,
    int DeactivatedUsers);

/// <summary>
/// Girişimi ve ona bağlı tüm kayıtları pasife alır.
///
/// Global sorgu süzgeçleri yalnızca <em>okumayı</em> daraltır; bir kaydın
/// silinmiş sayılması bağlı satırlarını kendiliğinden gizlemez. Bu yüzden
/// zincir burada elle yürütülüyor: aksi hâlde girişim listeden kalkar ama ekip
/// üyeleri, katılımları ve önerileri sistemde görünmeye devam ederdi.
///
/// Kayıtlar silinmiyor, işaretleniyor — denetim izi ve KVKK gerekçesi
/// <see cref="Domain.Common.ISoftDelete"/> üzerinde açıklandı.
/// </summary>
public sealed class DeleteStartupHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IAuditWriter audit)
{
    public async Task<Result<DeleteStartupResponse>> Handle(Guid id, CancellationToken ct)
    {
        // Program Yöneticisi kendi kapsamındaki girişimi düzenleyebilir ama
        // silemez: kapsam dışına taşan sonuçları olan tek yazma işlemi bu.
        if (currentUser.Role != UserRole.SuperAdmin)
            return Error.Forbidden("Girişim kaydını yalnızca sistem yöneticisi kapatabilir.");

        var startup = await db.Startups.FirstOrDefaultAsync(s => s.Id == id, ct);

        if (startup is null)
            return Error.NotFound("Girişim bulunamadı.");

        var teamMembers = await MarkAsync(db.TeamMembers.Where(x => x.StartupId == id), ct);
        var participations = await MarkAsync(
            db.ProgramParticipations.Where(x => x.StartupId == id), ct);
        var milestones = await MarkAsync(db.Milestones.Where(x => x.StartupId == id), ct);
        var achievements = await MarkAsync(db.Achievements.Where(x => x.StartupId == id), ct);
        var documents = await MarkAsync(db.Documents.Where(x => x.StartupId == id), ct);

        // Bekleyen öneriler de kapanıyor: onay kuyruğunda hedefi olmayan satır
        // kalmamalı, onaylanırsa "artık mevcut değil" hatasına düşerdi.
        var changeRequests = await MarkAsync(db.ChangeRequests.Where(x => x.StartupId == id), ct);

        // Portal kullanıcıları erişimden düşüyor ama girişim bağı korunuyor:
        // bağı silmek denetim izindeki "kim adına yazdı" bilgisini koparır.
        var portalUsers = await db.Users
            .Where(u => u.StartupId == id && u.IsActive)
            .ToListAsync(ct);

        foreach (var user in portalUsers)
            user.IsActive = false;

        var before = StartupAuditSnapshot.Of(startup);
        startup.IsDeleted = true;

        await db.SaveChangesAsync(ct);

        await audit.WriteAsync(
            "Startup.Delete", nameof(Startup), startup.Id,
            before: before,
            after: new
            {
                startup.IsDeleted,
                TeamMembers = teamMembers,
                Participations = participations,
                Milestones = milestones,
                Achievements = achievements,
                Documents = documents,
                ChangeRequests = changeRequests,
                DeactivatedUsers = portalUsers.Count
            },
            ct: ct);

        return new DeleteStartupResponse(
            startup.Id, startup.Name,
            teamMembers, participations, milestones, achievements,
            documents, changeRequests, portalUsers.Count);
    }

    /// <summary>
    /// Bağlı satırları işaretler. Toplu güncelleme (<c>ExecuteUpdate</c>)
    /// kullanılmıyor: o API sağlayıcıya özgü ve Application katmanı sağlayıcı
    /// bilmiyor. Ekosistem ölçeğinde bir girişimin bağlı kayıt sayısı küçük,
    /// bellekte işaretlemek kabul edilebilir.
    /// </summary>
    private async Task<int> MarkAsync<T>(IQueryable<T> query, CancellationToken ct)
        where T : class, Domain.Common.ISoftDelete
    {
        var rows = await query.ToListAsync(ct);

        foreach (var row in rows)
            row.IsDeleted = true;

        return rows.Count;
    }
}
