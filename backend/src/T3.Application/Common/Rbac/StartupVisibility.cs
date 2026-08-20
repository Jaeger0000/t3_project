using T3.Application.Common.Interfaces;
using T3.Domain.Identity;

namespace T3.Application.Common.Rbac;

/// <summary>
/// Tek bir girişim için hangi hassas alanların görünebileceğini belirler (KVKK).
/// Maskeleme kararı burada bir kez hesaplanır; mapper'lar yalnızca bu kaydı okur.
/// Yetkisiz alan boş string değil <c>null</c> döner — böylece arayüz
/// "veri yok" ile "yetkiniz yok" ayrımını yapabilir.
/// </summary>
public sealed record StartupVisibility(
    bool ShowContactDetails,
    bool ShowTaxNumber,
    bool ShowExactAmounts,
    bool ShowTeamPersonalData,
    bool ShowDocuments)
{
    public static StartupVisibility For(ICurrentUser user, Guid startupId)
    {
        if (!user.IsAuthenticated)
            return None;

        return user.Role switch
        {
            UserRole.SuperAdmin => All,

            UserRole.ProgramManager => new StartupVisibility(
                ShowContactDetails: true,
                ShowTaxNumber: false,      // vergi no yalnızca SuperAdmin ve girişimin kendisi
                ShowExactAmounts: true,
                ShowTeamPersonalData: true,
                ShowDocuments: true),

            // Girişim kendi verisinin tamamını görür.
            UserRole.StartupUser when user.StartupId == startupId => All,

            // Karar Verici: satırların tamamını görür, hassas alanları görmez.
            // Finansallar yalnızca agregat raporlarda sunulur.
            UserRole.DecisionMaker => new StartupVisibility(
                ShowContactDetails: false,
                ShowTaxNumber: false,
                ShowExactAmounts: false,
                ShowTeamPersonalData: false,
                ShowDocuments: false),

            _ => None
        };
    }

    public static StartupVisibility All { get; } = new(true, true, true, true, true);
    public static StartupVisibility None { get; } = new(false, false, false, false, false);
}
