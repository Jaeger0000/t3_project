using T3.Domain.Identity;

namespace T3.Application.Features.Auth;

/// <summary>
/// Oturum sahibinin arayüze dönen görünümü. Login ve /api/me aynı şekli
/// paylaşır — böylece frontend'de tek bir oturum tipi olur. Dilimler arası
/// DTO paylaşımından kaçınıyoruz ama bu ikisi aynı özelliğin iki ucu.
/// </summary>
public sealed record SessionUserResponse(
    Guid Id,
    string Email,
    string FullName,
    UserRole Role,
    Guid? StartupId,
    string? StartupName,
    IReadOnlyList<AssignedProgramResponse> Programs,

    /// <summary>
    /// Yönetici şifre atadıysa true. Arayüz bu bayrakla kullanıcıyı şifre
    /// değiştirme ekranına kilitler; sunucu tarafında engel değil, çünkü
    /// yöneticinin bildiği şifreyle yapılabilecek her şey zaten rolün yetkisi
    /// kadar — asıl amaç şifrenin ikinci sahibini ortadan kaldırmak.
    /// </summary>
    bool MustChangePassword,

    SessionPermissions Permissions);

public sealed record AssignedProgramResponse(Guid Id, string Name);

/// <summary>
/// Rolden türeyen yetki bayrakları. Arayüz bunlara bakarak düğme gösterir;
/// gerçek yetki kontrolü her zaman sunucuda (IStartupScope + policy) yapılır.
/// Buradaki değerler yalnızca gereksiz ekran öğesini saklamak içindir.
/// </summary>
public sealed record SessionPermissions(
    bool CanManageStartups,

    /// <summary>Program tanımı — kapsamın kendisi, yalnızca SuperAdmin.</summary>
    bool CanManagePrograms,

    /// <summary>Dönem ve katılım işlemleri; Program Yöneticisi kendi programında.</summary>
    bool CanManageProgramTerms,

    bool CanReviewApprovals,
    bool CanManageUsers,
    bool CanSeeExactAmounts,
    bool CanSeeContactDetails,
    bool MustSubmitForApproval);

public static class SessionUserMapper
{
    public static SessionUserResponse Map(
        User user, IReadOnlyList<AssignedProgramResponse> programs) =>
        new(
            user.Id,
            user.Email,
            user.FullName,
            user.Role,
            user.StartupId,
            user.Startup?.Name,
            programs,
            user.MustChangePassword,
            Permissions(user.Role));

    private static SessionPermissions Permissions(UserRole role) => new(
        CanManageStartups: role is UserRole.SuperAdmin or UserRole.ProgramManager,
        CanManagePrograms: role is UserRole.SuperAdmin,
        CanManageProgramTerms: role is UserRole.SuperAdmin or UserRole.ProgramManager,
        CanReviewApprovals: role is UserRole.SuperAdmin or UserRole.ProgramManager,
        CanManageUsers: role is UserRole.SuperAdmin,
        CanSeeExactAmounts: role is not UserRole.DecisionMaker,
        CanSeeContactDetails: role is not UserRole.DecisionMaker,
        MustSubmitForApproval: role is UserRole.StartupUser);
}
