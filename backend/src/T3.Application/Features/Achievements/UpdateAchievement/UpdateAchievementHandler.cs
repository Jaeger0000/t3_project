using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Rbac;
using T3.Application.Common.Results;
using T3.Application.Features.Startups;
using T3.Domain.Achievements;

namespace T3.Application.Features.Achievements.UpdateAchievement;

public sealed class UpdateAchievementHandler(
    IAppDbContext db,
    StartupEditGuard guard,
    IAuditWriter audit)
{
    public async Task<Result<AchievementResponse>> Handle(
        Guid startupId, Guid achievementId, AchievementWriteModel model, CancellationToken ct)
    {
        var access = await guard.ResolveEditableAsync(startupId, ct);
        if (!access.IsSuccess)
            return access.Error!;

        // StartupId koşulu korunuyor: kapsamdaki bir girişim üzerinden başka
        // girişimin kaydı güncellenemesin.
        var entity = await db.Achievements
            .FirstOrDefaultAsync(a => a.Id == achievementId && a.StartupId == startupId, ct);

        if (entity is null)
            return Error.NotFound("Başarı kaydı bulunamadı.");

        var currentKind = AchievementKinds.Of(entity);

        // Tür değiştirilemez: TPH ayrıştırıcı kolonu kaydın kimliğinin parçası,
        // yerinde değiştirilemez. Doğru davranış eskisini silip yenisini
        // eklemek; bunu sessizce yapmak yerine açıkça reddediyoruz.
        if (model.Kind != currentKind)
            return Error.Conflict(
                $"Kayıt türü değiştirilemez ({AchievementLabels.Kind(currentKind)}). "
                + "Kaydı silip yenisini ekleyin.");

        var before = entity.ToResponse(StartupVisibility.All);
        model.ApplyTo(entity);

        await db.SaveChangesAsync(ct);

        var after = entity.ToResponse(StartupVisibility.All);

        await audit.WriteAsync(
            "Achievement.Update", nameof(Achievement), entity.Id,
            before: before, after: after, ct: ct);

        return after;
    }
}
