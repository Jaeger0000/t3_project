using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Application.Common.Text;
using T3.Application.Features.Users;
using T3.Domain.Identity;
using T3.Domain.Registrations;
using T3.Domain.Startups;

namespace T3.Application.Features.Registrations.ApproveRegistrationRequest;

public sealed record ApproveRegistrationResponse(Guid StartupId, Guid UserId);

/// <summary>
/// Onay: tek başvurudan aynı anda bir <see cref="Startup"/> ve bir
/// <see cref="User"/> doğar. Girişim kullanıcısı hiçbir tabloya doğrudan
/// yazamadığı için bu handler, portalın "kendi kendine kayıt" yolunun tek
/// gerçek yazma noktası.
/// </summary>
public sealed class ApproveRegistrationRequestHandler(
    IAppDbContext db,
    UserAdminGuard guard,
    ICurrentUser currentUser,
    IAuditWriter audit)
{
    public async Task<Result<ApproveRegistrationResponse>> Handle(Guid id, CancellationToken ct)
    {
        if (guard.EnsureCanManage() is { } denied)
            return denied;

        var registration = await db.StartupRegistrationRequests
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (registration is null)
            return Error.NotFound("Başvuru bulunamadı.");

        if (registration.Status != RegistrationRequestStatus.Pending)
            return Error.Conflict("Bu başvuru zaten sonuçlandırılmış.");

        // Yarış durumu: başvurudan onaya kadar geçen sürede biri aynı
        // e-posta/isimle admin eliyle kayıt açmış olabilir. Başvuru anında
        // yapılan kontrol burada tekrarlanmadan onay sessizce çakışan bir
        // hesap/girişim üretirdi.
        if (await guard.EnsureEmailAvailableAsync(registration.Email, null, ct) is { } emailTaken)
            return emailTaken;

        var normalizedStartupName = SearchText.Normalize(registration.StartupName);
        var startupNameTaken = await db.Startups
            .IgnoreQueryFilters()
            .AnyAsync(s => s.Name.ToLower() == normalizedStartupName && !s.IsDeleted, ct);

        if (startupNameTaken)
            return Error.Conflict($"\"{registration.StartupName}\" adlı bir girişim zaten kayıtlı.");

        var startup = new Startup
        {
            Name = registration.StartupName,
            Sector = registration.Sector,
            City = registration.City,
            ContactEmail = registration.Email,
            ContactPhone = registration.ContactPhone,
            Status = StartupStatus.Active
        };

        db.Startups.Add(startup);

        var user = new User
        {
            Email = registration.Email,
            FullName = registration.FullName,
            Role = UserRole.StartupUser,
            StartupId = startup.Id,
            PasswordHash = registration.PasswordHash,
            IsActive = true,

            // Admin-oluşturma akışının aksine burada MustChangePassword=false:
            // kullanıcı zaten kendi şifresini formda seçti, "geçici şifre"
            // kavramı bu yolda yok — ilk girişte kendi şifresini tekrar
            // koymaya zorlamak anlamsız bir ekstra adım olurdu.
            MustChangePassword = false
        };

        db.Users.Add(user);

        registration.Status = RegistrationRequestStatus.Approved;
        registration.ReviewedAt = DateTimeOffset.UtcNow;
        registration.ReviewedByUserId = currentUser.UserId;
        registration.CreatedStartupId = startup.Id;
        registration.CreatedUserId = user.Id;

        // Üç değişiklik (girişim, kullanıcı, başvuru durumu) tek transaction'da:
        // biri düşerse hiçbiri kalıcı olmamalı — yarım kalmış bir onay, şifre
        // hash'i bir yerde asılı kalmış kullanıcı üretirdi.
        await db.SaveChangesAsync(ct);

        // Şifre/hash yine burada da yok — iz yalnızca "hangi girişim ve
        // kullanıcı doğdu" bilgisini taşır.
        await audit.WriteAsync(
            "StartupRegistration.Approve", nameof(StartupRegistrationRequest), registration.Id,
            after: new { StartupId = startup.Id, UserId = user.Id, startup.Name, user.Email },
            ct: ct);

        return new ApproveRegistrationResponse(startup.Id, user.Id);
    }
}
