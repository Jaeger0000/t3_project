using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Rbac;
using T3.Application.Common.Results;

namespace T3.Application.Features.Achievements.ListAchievements;

/// <summary>
/// Girişimin başarı ve finans kayıtları (MVP #4).
///
/// <paramref name="ExactAmountsVisible"/> liste düzeyinde de dönüyor: arayüz
/// tek tek satırlara bakmadan "bu rolde tutarlar gizli" başlığını gösterebilsin.
/// </summary>
public sealed record AchievementListResponse(
    Guid StartupId,
    bool ExactAmountsVisible,
    IReadOnlyList<AchievementResponse> Items);

public sealed class ListAchievementsHandler(
    IAppDbContext db,
    IStartupScope scope,
    ICurrentUser currentUser)
{
    public async Task<Result<AchievementListResponse>> Handle(
        Guid startupId, AchievementKind? kind, CancellationToken ct)
    {
        // Kapsam dışındaki girişim "yok" sayılır; 403 varlığını sızdırırdı.
        var exists = await scope.Apply(db.Startups.AsNoTracking())
            .AnyAsync(s => s.Id == startupId, ct);

        if (!exists)
            return Error.NotFound("Girişim bulunamadı.");

        var visibility = StartupVisibility.For(currentUser, startupId);

        // Tür süzgeci bellekte uygulanıyor: TPH ayrıştırıcısı üzerinden
        // sorgulamak sağlayıcıya özel API gerektirir, tek girişimin kayıt
        // sayısı ise bir düzineyi geçmiyor.
        var rows = await db.Achievements.AsNoTracking()
            .Where(a => a.StartupId == startupId)
            .OrderByDescending(a => a.OccurredOn)
            .ToListAsync(ct);

        var items = rows
            .Select(a => a.ToResponse(visibility))
            .Where(a => kind is null || a.Kind == kind)
            .ToList();

        return new AchievementListResponse(startupId, visibility.ShowExactAmounts, items);
    }
}
