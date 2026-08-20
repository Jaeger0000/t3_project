using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Rbac;
using T3.Application.Common.Results;
using T3.Domain.Startups;

namespace T3.Application.Features.Startups;

/// <summary>
/// "Bu girişim kapsamımda mı ve doğrudan yazabilir miyim?" kontrolünün tek
/// uygulaması. Girişim altındaki her yazma dilimi (ekip, başarı, doküman)
/// aynı iki adımı ister; kopyalamak bir dilimde kontrolü atlama riski doğurur.
/// </summary>
public sealed class StartupEditGuard(IAppDbContext db, IStartupScope scope)
{
    public async Task<Result<Startup>> ResolveEditableAsync(
        Guid startupId, CancellationToken ct)
    {
        var startup = await scope.Apply(db.Startups)
            .FirstOrDefaultAsync(s => s.Id == startupId, ct);

        if (startup is null)
            return Error.NotFound("Girişim bulunamadı.");

        if (!scope.CanEditDirectly(startup.Id))
            return Error.Forbidden(
                "Doğrudan düzenleme yetkiniz yok. Değişikliğinizi onay isteği olarak gönderin.");

        return startup;
    }
}
