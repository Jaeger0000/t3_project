using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Domain.Identity;

namespace T3.Application.Features.Programs;

/// <summary>
/// Program dilimlerinin ortak yetki kontrolü.
///
/// İki farklı kapı var ve karıştırılmamaları önemli: <em>programın kendisi</em>
/// (oluşturma, düzenleme, kapatma) yalnızca sistem yöneticisinin işi çünkü
/// program listesi aynı zamanda yetki kapsamının tanımı — kendi kapsamını
/// büyütebilen bir Program Yöneticisi RBAC'ı anlamsız kılar. <em>Dönem ve
/// katılım</em> ise günlük operasyon; Program Yöneticisi bunları yapar ama
/// yalnızca kendi programında.
///
/// Kontrol handler içinde tekrar ediliyor (uç noktadaki politikaya ek olarak):
/// MCP araçları handler'ları doğrudan çağırdığında politika hattı atlanır.
/// </summary>
public sealed class ProgramAccessGuard(IAppDbContext db, ICurrentUser currentUser)
{
    public Error? EnsureCanManagePrograms() =>
        currentUser.Role == UserRole.SuperAdmin
            ? null
            : Error.Forbidden("Program tanımlarını yalnızca sistem yöneticisi yönetebilir.");

    /// <summary>
    /// Dönem/katılım işlemlerinin kapısı. Programın var olduğunu da doğrular:
    /// "bulunamadı" ile "yetkiniz yok" ayrımı çağıran dilimde anlamlı kalsın.
    /// </summary>
    public async Task<Error?> EnsureOwnsProgramAsync(Guid programId, CancellationToken ct)
    {
        if (currentUser.Role is not (UserRole.SuperAdmin or UserRole.ProgramManager))
            return Error.Forbidden("Program dönemi yönetme yetkiniz yok.");

        var exists = await db.Programs.AnyAsync(p => p.Id == programId, ct);

        if (!exists)
            return Error.NotFound("Program bulunamadı.");

        if (currentUser.Role == UserRole.ProgramManager
            && !currentUser.AssignedProgramIds.Contains(programId))
            return Error.Forbidden("Bu program sizin sorumluluğunuzda değil.");

        return null;
    }
}
