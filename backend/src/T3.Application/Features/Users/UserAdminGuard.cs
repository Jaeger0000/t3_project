using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Application.Common.Text;
using T3.Domain.Identity;

namespace T3.Application.Features.Users;

/// <summary>
/// Kullanıcı yönetiminin dilimler arası ön kontrolleri. Rol ile kapsam bağı
/// (girişim / program) yetkilendirmenin temeli olduğu için tek noktada
/// doğrulanıyor: oluşturma ve güncelleme aynı kuralı iki kez yazarsa biri
/// gevşer, kapsamsız bir Program Yöneticisi ya da girişimsiz bir portal
/// kullanıcısı sisteme sızar.
/// </summary>
public sealed class UserAdminGuard(IAppDbContext db, ICurrentUser currentUser)
{
    /// <summary>
    /// Uç noktadaki politikanın handler tarafındaki eşi. MCP tool çağrısı
    /// politika ardışık düzeninden geçmediği için burada da duruyor.
    /// </summary>
    public Error? EnsureCanManage() =>
        currentUser.Role == UserRole.SuperAdmin
            ? null
            : Error.Forbidden("Kullanıcı yönetimi yalnızca sistem yöneticisine açıktır.");

    /// <summary>
    /// Kendi hesabını kilitlemeyi engeller. Tek SuperAdmin'in kendini pasife
    /// alması ya da rolünü düşürmesi sistemi yönetilemez bırakır; geri dönüş
    /// yolu veritabanına elle müdahaleden geçer.
    /// </summary>
    public Error? EnsureNotSelf(Guid userId, string action) =>
        currentUser.UserId == userId
            ? Error.Validation($"Kendi hesabınız için {action} yapamazsınız.")
            : null;

    /// <summary>
    /// Rol ile kapsam bağının tutarlılığı. Fazlalık alan sessizce yok
    /// sayılmıyor, hata olarak dönüyor: "program yöneticisi yaptım ama girişim
    /// alanını da doldurdum" durumunda hangisinin geçerli olduğu belirsiz kalır.
    /// </summary>
    public static Error? ValidateBinding(
        UserRole role, Guid? startupId, IReadOnlyList<Guid>? programIds)
    {
        var programCount = programIds?.Count ?? 0;

        switch (role)
        {
            case UserRole.StartupUser when startupId is null:
                return Error.Validation("Girişim kullanıcısı için girişim seçilmelidir.");

            case UserRole.StartupUser when programCount > 0:
                return Error.Validation("Girişim kullanıcısına program ataması yapılamaz.");

            case UserRole.ProgramManager when programCount == 0:
                return Error.Validation("Program yöneticisine en az bir program atanmalıdır.");

            case UserRole.ProgramManager when startupId is not null:
                return Error.Validation("Program yöneticisi bir girişime bağlanamaz.");

            case UserRole.SuperAdmin or UserRole.DecisionMaker
                when startupId is not null || programCount > 0:
                return Error.Validation(
                    $"{UserLabels.Role(role)} rolüne girişim veya program ataması yapılamaz.");

            default:
                return null;
        }
    }

    /// <summary>
    /// E-posta tekilliği. Silinmiş kayıtlar da sayılıyor: tekillik kısıtı
    /// veritabanında soft-delete'ten bağımsız, aksi hâlde doğrulamayı geçen
    /// kayıt insert sırasında patlardı.
    /// </summary>
    public async Task<Error?> EnsureEmailAvailableAsync(
        string email, Guid? excludeUserId, CancellationToken ct)
    {
        var normalized = SearchText.Normalize(email);

        // Kendi kaydını dışlama koşulu ayrı satırda: tek ifadede
        // "u.Id != excludeUserId" yazmak null karşılaştırma anlamına bel
        // bağlamak olurdu, oluşturma akışında dışlanacak kimlik yok.
        var query = db.Users
            .IgnoreQueryFilters()
            .Where(u => u.Email.ToLower() == normalized);

        if (excludeUserId is { } excluded)
            query = query.Where(u => u.Id != excluded);

        var taken = await query.AnyAsync(ct);

        return taken
            ? Error.Conflict($"\"{email.Trim()}\" adresiyle kayıtlı bir kullanıcı zaten var.")
            : null;
    }

    public async Task<Error?> EnsureStartupExistsAsync(Guid? startupId, CancellationToken ct)
    {
        if (startupId is not { } id)
            return null;

        var exists = await db.Startups.AnyAsync(s => s.Id == id, ct);
        return exists ? null : Error.Validation("Seçilen girişim bulunamadı.");
    }

    /// <summary>
    /// Program atamalarını istenen listeye eşitler.
    ///
    /// Kaldırılan atama silinmiyor, işaretleniyor: denetim izi "bu kullanıcı bir
    /// dönem şu programa yetkiliydi" bilgisini kaybetmemeli. Aynı sebeple daha
    /// önce kaldırılmış bir atama yeniden eklenirken satır diriltiliyor —
    /// (UserId, ProgramId) tekil olduğu için ikinci satır eklenemez.
    /// </summary>
    public async Task<Error?> SyncProgramsAsync(
        User user, IReadOnlyList<Guid>? programIds, CancellationToken ct)
    {
        var wanted = programIds?.Distinct().ToList() ?? [];

        if (wanted.Count > 0)
        {
            var known = await db.Programs
                .Where(p => wanted.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync(ct);

            if (known.Count != wanted.Count)
                return Error.Validation("Seçilen programlardan biri bulunamadı.");
        }

        var existing = await db.UserProgramAssignments
            .IgnoreQueryFilters()
            .Where(a => a.UserId == user.Id)
            .ToListAsync(ct);

        foreach (var assignment in existing)
        {
            var keep = wanted.Contains(assignment.ProgramId);

            if (keep && assignment.IsDeleted)
            {
                assignment.IsDeleted = false;
                assignment.DeletedAt = null;
                assignment.AssignedAt = DateTimeOffset.UtcNow;
            }
            else if (!keep && !assignment.IsDeleted)
            {
                assignment.IsDeleted = true;
            }
        }

        var existingProgramIds = existing.Select(a => a.ProgramId).ToHashSet();

        foreach (var programId in wanted.Where(id => !existingProgramIds.Contains(id)))
            db.UserProgramAssignments.Add(new UserProgramAssignment
            {
                UserId = user.Id,
                ProgramId = programId,
                AssignedAt = DateTimeOffset.UtcNow
            });

        return null;
    }

    /// <summary>
    /// Kaydı yanıta çevirmek için gereken ilişkileri yükler. Yazma
    /// handler'larının hepsi aynı şekli döndüğü için ortak.
    /// </summary>
    public async Task<UserResponse> LoadResponseAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.Startup)
            .Include(u => u.ProgramAssignments)
                .ThenInclude(a => a.Program)
            .FirstAsync(u => u.Id == userId, ct);

        return user.ToResponse();
    }
}
