using T3.Api.Authorization;
using T3.Api.Filters;
using T3.Api.Http;
using T3.Application.Features.Achievements;
using T3.Application.Features.Achievements.AddAchievement;
using T3.Application.Features.Achievements.DeleteAchievement;
using T3.Application.Features.Achievements.ListAchievements;
using T3.Application.Features.Achievements.UpdateAchievement;

namespace T3.Api.Endpoints;

/// <summary>
/// Başarı ve finans kayıtları (MVP #4). Yazma uçları yalnızca yetkiliye açık;
/// girişim kullanıcısı aynı kayıtları onay isteği olarak gönderir
/// (POST /api/change-requests).
/// </summary>
public static class AchievementEndpoints
{
    public static IEndpointRouteBuilder MapAchievementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/startups/{id:guid}/achievements").WithTags("Achievements");

        group.MapGet("/", async (
                Guid id,
                AchievementKind? kind,
                ListAchievementsHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, kind, ct)).ToHttp())
            .WithSummary("Girişimin başarı ve finans kayıtlarını döner (tutarlar role göre maskelenir).");

        group.MapPost("/", async (
                Guid id,
                AchievementWriteModel model,
                AddAchievementHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, model, ct))
                .ToCreated(created => $"/api/startups/{id}/achievements/{created.Id}"))
            .RequireAuthorization(Policies.ManageStartups)
            .WithValidation<AchievementWriteModel>()
            .WithSummary("Başarı/finans kaydı ekler.");

        group.MapPut("/{achievementId:guid}", async (
                Guid id,
                Guid achievementId,
                AchievementWriteModel model,
                UpdateAchievementHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, achievementId, model, ct)).ToHttp())
            .RequireAuthorization(Policies.ManageStartups)
            .WithValidation<AchievementWriteModel>()
            .WithSummary("Başarı/finans kaydını günceller.");

        group.MapDelete("/{achievementId:guid}", async (
                Guid id,
                Guid achievementId,
                DeleteAchievementHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, achievementId, ct)).ToNoContent())
            .RequireAuthorization(Policies.ManageStartups)
            .WithSummary("Başarı/finans kaydını pasife alır.");

        return app;
    }
}
