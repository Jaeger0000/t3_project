using T3.Api.Authorization;
using T3.Api.Filters;
using T3.Api.Http;
using T3.Application.Features.Users.CreateUser;
using T3.Application.Features.Users.DeactivateUser;
using T3.Application.Features.Users.ListUsers;
using T3.Application.Features.Users.SetUserPassword;
using T3.Application.Features.Users.UpdateUser;
using T3.Domain.Identity;

namespace T3.Api.Endpoints;

/// <summary>
/// Kullanıcı ve rol yönetimi — yetki matrisinde yalnızca SuperAdmin'e açık.
/// Grubun tamamı tek politikayla korunuyor; handler'lar aynı kontrolü
/// <c>UserAdminGuard</c> üzerinden bağımsız olarak tekrarlıyor.
/// </summary>
public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization(Policies.ManageUsers);

        group.MapGet("/", async (
                string? q,
                UserRole? role,
                bool? isActive,
                Guid? programId,
                Guid? startupId,
                int? page,
                int? pageSize,
                ListUsersHandler handler,
                CancellationToken ct) =>
            {
                var request = new ListUsersRequest
                {
                    Q = q,
                    Role = role,
                    IsActive = isActive,
                    ProgramId = programId,
                    StartupId = startupId,
                    Page = page ?? 1,
                    PageSize = pageSize ?? 20
                };

                return (await handler.Handle(request, ct)).ToHttp();
            })
            .WithSummary("Kullanıcıları listeler ve süzer.");

        group.MapPost("/", async (
                CreateUserRequest request,
                CreateUserHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(request, ct))
                .ToCreated(created => $"/api/users/{created.Id}"))
            .WithValidation<CreateUserRequest>()
            .WithSummary("Yeni kullanıcı oluşturur ve rol kapsamını atar.");

        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateUserRequest request,
                UpdateUserHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, request, ct)).ToHttp())
            .WithValidation<UpdateUserRequest>()
            .WithSummary("Kullanıcının adını, rolünü, kapsamını ve durumunu günceller.");

        // Şifre ayrı uçta: profil düzenlemesiyle aynı istekte taşınmıyor,
        // denetim izinde de ayrı bir eylem olarak görünüyor.
        group.MapPut("/{id:guid}/password", async (
                Guid id,
                SetUserPasswordRequest request,
                SetUserPasswordHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, request, ct)).ToHttp())
            .WithValidation<SetUserPasswordRequest>()
            .WithSummary("Kullanıcıya yeni şifre atar.");

        group.MapDelete("/{id:guid}", async (
                Guid id,
                DeactivateUserHandler handler,
                CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToNoContent())
            .WithSummary("Hesabı pasife alır (kayıt silinmez; denetim izi korunur).");

        return app;
    }
}
