using T3.Application.Common.Interfaces;
using T3.Domain.Identity;
using T3.Domain.Startups;

namespace T3.Application.Common.Rbac;

public sealed class StartupScope(ICurrentUser user) : IStartupScope
{
    public IQueryable<Startup> Apply(IQueryable<Startup> query)
    {
        if (!user.IsAuthenticated)
            return query.Where(_ => false);

        switch (user.Role)
        {
            case UserRole.SuperAdmin:
            case UserRole.DecisionMaker:
                // Tüm ekosistemi görür. Karar Verici'nin kısıtı satır değil
                // alan düzeyinde: hassas alanlar StartupVisibility ile maskelenir.
                return query;

            case UserRole.StartupUser:
                var ownId = user.StartupId;
                return ownId is null
                    ? query.Where(_ => false)
                    : query.Where(s => s.Id == ownId);

            case UserRole.ProgramManager:
                // Yalnızca sorumlu olduğu programlardan geçmiş girişimler.
                var programIds = user.AssignedProgramIds.ToArray();
                return programIds.Length == 0
                    ? query.Where(_ => false)
                    : query.Where(s => s.Participations
                        .Any(p => programIds.Contains(p.ProgramTerm.ProgramId)));

            default:
                return query.Where(_ => false);
        }
    }

    public bool CanEditDirectly(Guid startupId) => user.Role switch
    {
        UserRole.SuperAdmin => true,
        // Program Yöneticisi kapsam içindeyse düzenleyebilir; kapsam kontrolü
        // çağıran handler'da Apply() ile yapılır, burada rol yetkisi verilir.
        UserRole.ProgramManager => true,
        // Girişimin kendisi doğrudan yazamaz — ChangeRequest üzerinden gider.
        _ => false
    };

    public bool CanReviewApprovals =>
        user.Role is UserRole.SuperAdmin or UserRole.ProgramManager;
}
