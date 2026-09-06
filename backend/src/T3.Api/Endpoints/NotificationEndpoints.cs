using T3.Api.Authorization;
using T3.Api.Http;
using T3.Application.Features.Notifications.DeleteNotification;
using T3.Application.Features.Notifications.ListMyNotifications;
using T3.Application.Features.Notifications.ListSentNotifications;
using T3.Application.Features.Notifications.MarkNotificationRead;
using T3.Application.Features.Notifications.MarkNotificationsRead;
using T3.Application.Features.Notifications.RestoreNotification;

namespace T3.Api.Endpoints;

/// <summary>
/// Bildirim gelen kutusu ve SuperAdmin gözetim ekranı. Gönderme ucu
/// (<c>POST /api/startups/{id}/notifications</c>) girişim kapsamlı olduğu
/// için burada değil, <c>StartupEndpoints</c>'te duruyor.
/// </summary>
public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications").WithTags("Notifications");

        // Politika yok: herhangi bir kimlik doğrulanmış kullanıcı kendi
        // bildirimlerini görebilir, sahiplik handler içinde RecipientUserId
        // ile daraltılıyor.
        group.MapGet("/", async (
                ListMyNotificationsHandler handler, CancellationToken ct) =>
            (await handler.Handle(ct)).ToHttp())
            .WithSummary("Oturum sahibinin kendi bildirimlerini ve okunmamış sayısını döner.");

        group.MapPost("/read-all", async (
                MarkNotificationsReadHandler handler, CancellationToken ct) =>
            (await handler.Handle(ct)).ToHttp())
            .WithSummary("Oturum sahibinin tüm bildirimlerini okunmuş işaretler.");

        group.MapPost("/{id:guid}/read", async (
                Guid id,
                MarkNotificationReadHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToNoContent())
            .WithSummary("Tek bir bildirimi okunmuş işaretler.");

        group.MapDelete("/{id:guid}", async (
                Guid id,
                DeleteNotificationHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToNoContent())
            .WithSummary("Bildirimi oturum sahibinin gelen kutusundan siler (kayıt kalıcı, yalnızca görünürlük değişir).");

        group.MapPost("/{id:guid}/restore", async (
                Guid id,
                RestoreNotificationHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToNoContent())
            .WithSummary("Silinenler sekmesinden bir bildirimi geri yükler.");

        group.MapGet("/sent", async (
                ListSentNotificationsHandler handler, CancellationToken ct) =>
            (await handler.Handle(ct)).ToHttp())
            .RequireAuthorization(Policies.ViewAllNotifications)
            .WithSummary("Tüm bildirimleri gönderen role göre ayırarak döner (yalnızca SuperAdmin).");

        return app;
    }
}
