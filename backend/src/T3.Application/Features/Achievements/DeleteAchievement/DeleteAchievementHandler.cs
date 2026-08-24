using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Rbac;
using T3.Application.Common.Results;
using T3.Application.Features.Startups;
using T3.Domain.Achievements;

namespace T3.Application.Features.Achievements.DeleteAchievement;

/// <summary>
/// Kaydı pasife alır. Fiziksel silme yok: geçmiş finansal veri denetim izinin
/// dayanağı, satır kaybolursa "bu tutar neden değişti" sorusu cevapsız kalır.
/// </summary>
public sealed class DeleteAchievementHandler(
    IAppDbContext db,
    StartupEditGuard guard,
    IAuditWriter audit)
{
    public async Task<Result<bool>> Handle(
        Guid startupId, Guid achievementId, CancellationToken ct)
    {
        var access = await guard.ResolveEditableAsync(startupId, ct);
        if (!access.IsSuccess)
            return access.Error!;

        var entity = await db.Achievements
            .FirstOrDefaultAsync(a => a.Id == achievementId && a.StartupId == startupId, ct);

        if (entity is null)
            return Error.NotFound("Başarı kaydı bulunamadı.");

        var before = entity.ToResponse(StartupVisibility.All);

        entity.IsDeleted = true;
        entity.DeletedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        await audit.WriteAsync(
            "Achievement.Delete", nameof(Achievement), entity.Id,
            before: before, ct: ct);

        return true;
    }
}
