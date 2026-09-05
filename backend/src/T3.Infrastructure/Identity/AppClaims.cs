namespace T3.Infrastructure.Identity;

/// <summary>
/// Jetona gömülen özel claim adları. Rolün yanında kapsam bilgisi de taşınır ki
/// her istekte kullanıcının program atamaları için veritabanına gidilmesin.
/// </summary>
public static class AppClaims
{
    public const string UserId = "t3:uid";
    public const string Role = "t3:role";
    public const string StartupId = "t3:startup";
    public const string ProgramIds = "t3:programs";

    /// <summary>Bkz. <see cref="T3.Domain.Identity.User.SecurityStamp"/>.</summary>
    public const string SecurityStamp = "t3:stamp";
}
