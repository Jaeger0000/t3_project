using T3.Application.Common.Interfaces;
using T3.Application.Common.Rbac;
using T3.Application.Common.Results;
using T3.Application.Features.Startups;
using T3.Domain.Achievements;

namespace T3.Application.Features.Achievements.AddAchievement;

/// <summary>
/// Başarı/finans kaydı ekler (doğrudan yazma yolu).
///
/// Bu yol yalnızca yetkiliye açık; girişim kullanıcısı aynı işi onay isteği
/// olarak gönderir. Bu yüzden buradan giren kayıt <em>doğrulanmış</em> sayılır:
/// kaydı yazan zaten yetkilinin kendisi.
/// </summary>
public sealed class AddAchievementHandler(
    IAppDbContext db,
    StartupEditGuard guard,
    ICurrentUser currentUser,
    IAuditWriter audit)
{
    public async Task<Result<AchievementResponse>> Handle(
        Guid startupId, AchievementWriteModel model, CancellationToken ct)
    {
        var access = await guard.ResolveEditableAsync(startupId, ct);
        if (!access.IsSuccess)
            return access.Error!;

        var entity = AchievementKinds.NewFor(model.Kind, startupId);
        model.ApplyTo(entity);

        entity.IsVerified = true;
        entity.VerifiedByUserId = currentUser.UserId;
        entity.VerifiedAt = DateTimeOffset.UtcNow;

        db.Achievements.Add(entity);
        await db.SaveChangesAsync(ct);

        var response = entity.ToResponse(StartupVisibility.All);

        await audit.WriteAsync(
            "Achievement.Create", nameof(Achievement), entity.Id,
            after: response, ct: ct);

        return response;
    }
}
