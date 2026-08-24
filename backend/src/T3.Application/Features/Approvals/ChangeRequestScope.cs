using T3.Application.Common.Interfaces;
using T3.Application.Common.Rbac;
using T3.Domain.Approvals;
using T3.Domain.Identity;

namespace T3.Application.Features.Approvals;

public sealed class ChangeRequestScope(
    ICurrentUser user,
    IAppDbContext db,
    IStartupScope startupScope) : IChangeRequestScope
{
    public IQueryable<ChangeRequest> Apply(IQueryable<ChangeRequest> query)
    {
        if (!user.IsAuthenticated)
            return query.Where(_ => false);

        switch (user.Role)
        {
            case UserRole.SuperAdmin:
                return query;

            case UserRole.ProgramManager:
                // Kapsam tanımı IStartupScope'ta duruyor, burada tekrar
                // edilmiyor. Sorgulanabilir alt sorgu olarak lambda'nın
                // DIŞINDA hesaplanmak zorunda: içeride çağrılsa EF metodu
                // çeviremeyip patlardı.
                var visibleStartups = startupScope.Apply(db.Startups);
                return query.Where(c => visibleStartups.Any(s => s.Id == c.StartupId));

            case UserRole.StartupUser:
                // Girişim kendi isteklerini görür — bekleyeni, onaylananı ve
                // ret gerekçesini. Portalın "isteğim ne oldu" ekranı bu.
                var ownId = user.StartupId;
                return ownId is null
                    ? query.Where(_ => false)
                    : query.Where(c => c.StartupId == ownId);

            default:
                // Karar Verici onay akışının tarafı değil (yetki matrisi).
                return query.Where(_ => false);
        }
    }

    /// <summary>
    /// Yalnızca girişim kullanıcısı öneri gönderir. Diğer roller doğrudan
    /// yazar; onlara bu kanalı açmak aynı işi iki yoldan yapılabilir hâle
    /// getirir ve "hiçbir girişim verisi onaysız yayına girmiyor" cümlesinin
    /// kimin için geçerli olduğunu bulanıklaştırırdı.
    /// </summary>
    public bool CanSubmit => user.Role is UserRole.StartupUser;

    public bool CanReview => user.Role is UserRole.SuperAdmin or UserRole.ProgramManager;
}
