using T3.Domain.Identity;

namespace T3.Application.Features.Users;

/// <summary>
/// Kullanıcı yönetimi ekranının satırı. Listeleme, oluşturma ve güncelleme aynı
/// şekli döner — üçü de aynı kaydın aynı görünümünü üretiyor, ayrı DTO tutmak
/// üçünü senkron tutma yükünden başka bir şey getirmezdi.
///
/// <c>PasswordHash</c> hiçbir koşulda bu şekle girmez.
/// </summary>
public sealed record UserResponse(
    Guid Id,
    string Email,
    string FullName,
    UserRole Role,
    string RoleLabel,
    Guid? StartupId,
    string? StartupName,
    IReadOnlyList<UserProgramResponse> Programs,
    bool IsActive,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt);

public sealed record UserProgramResponse(Guid Id, string Name);

public static class UserLabels
{
    public static string Role(UserRole role) => role switch
    {
        UserRole.SuperAdmin => "Sistem Yöneticisi",
        UserRole.ProgramManager => "Program Yöneticisi",
        UserRole.StartupUser => "Girişim Kullanıcısı",
        UserRole.DecisionMaker => "Karar Verici",
        _ => role.ToString()
    };
}

internal static class UserMapper
{
    /// <summary>
    /// Varlığı yanıta çevirir. Program atamaları soft-delete süzgecinden
    /// geçmiş koleksiyondan okunur; kaldırılmış atamalar listede görünmez.
    /// </summary>
    public static UserResponse ToResponse(this User user) => new(
        user.Id,
        user.Email,
        user.FullName,
        user.Role,
        UserLabels.Role(user.Role),
        user.StartupId,
        user.Startup?.Name,
        [.. user.ProgramAssignments
            .Where(a => !a.IsDeleted)
            .Select(a => new UserProgramResponse(a.ProgramId, a.Program?.Name ?? "—"))
            .OrderBy(p => p.Name)],
        user.IsActive,
        user.LastLoginAt,
        user.CreatedAt);
}
