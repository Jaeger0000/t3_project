using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Application.Common.Text;
using T3.Application.Features.Users;
using T3.Domain.Registrations;
using T3.Domain.Startups;

namespace T3.Application.Features.Auth.Register;

public sealed record RegisterStartupResponse(Guid Id);

/// <summary>
/// Girişimin kendi kendine kayıt uçtan noktası — kimlik istemez, herkese açık.
/// Hiçbir tabloya doğrudan yazmıyor: yalnızca onay bekleyen bir başvuru
/// oluşturuyor, gerçek Startup+kullanıcı satırı yalnızca
/// <see cref="Registrations.ApproveRegistrationRequest.ApproveRegistrationRequestHandler"/>
/// üzerinden doğar.
/// </summary>
public sealed class RegisterStartupHandler(
    IAppDbContext db,
    UserAdminGuard guard,
    IPasswordHasher passwordHasher,
    IAuditWriter audit)
{
    public async Task<Result<RegisterStartupResponse>> Handle(
        RegisterStartupRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        // Zaten bir hesabı olan biri ikinci kez "kayıt olamaz" — mevcut
        // kullanıcı tablosuna karşı aynı tekillik kontrolü, admin'in
        // CreateUser'da kullandığı kontrolle birebir aynı.
        if (await guard.EnsureEmailAvailableAsync(email, null, ct) is { } emailTaken)
            return emailTaken;

        // Aynı e-postayla bekleyen başka bir başvuru varsa ikinci kaydı
        // reddediyoruz: aksi hâlde SuperAdmin hangisini onaylayacağını
        // bilemeyen, birbirini geçersiz kılan iki satırla karşılaşırdı.
        var normalizedEmail = SearchText.Normalize(email);
        var pendingExists = await db.StartupRegistrationRequests
            .Where(r => r.Status == RegistrationRequestStatus.Pending)
            .AnyAsync(r => r.Email.ToLower() == normalizedEmail, ct);

        if (pendingExists)
            return Error.Conflict($"\"{request.Email.Trim()}\" adresiyle bekleyen bir başvuru zaten var.");

        var startupName = request.StartupName.Trim();
        var normalizedStartupName = SearchText.Normalize(startupName);

        // Girişim adı tekilliği CreateStartupHandler'daki kalıpla birebir
        // aynı: onay anına kadar admin eliyle aynı isim açılmışsa başvuru en
        // baştan reddedilsin, SuperAdmin'i onay sırasında şaşırtmasın.
        var startupNameTaken = await db.Startups
            .IgnoreQueryFilters()
            .AnyAsync(s => s.Name.ToLower() == normalizedStartupName && !s.IsDeleted, ct);

        if (startupNameTaken)
            return Error.Conflict($"\"{startupName}\" adlı bir girişim zaten kayıtlı.");

        var registration = new StartupRegistrationRequest
        {
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            FullName = request.FullName.Trim(),
            StartupName = startupName,
            Sector = request.Sector,
            City = request.City?.Trim(),
            ContactPhone = request.ContactPhone?.Trim(),
            Status = RegistrationRequestStatus.Pending
        };

        db.StartupRegistrationRequests.Add(registration);
        await db.SaveChangesAsync(ct);

        // Şifre/hash denetim izine asla girmez — iz yalnızca "böyle bir
        // başvuru geldi" bilgisini taşır.
        await audit.WriteAsync(
            "StartupRegistration.Submit", nameof(StartupRegistrationRequest), registration.Id,
            after: new
            {
                registration.Email,
                registration.FullName,
                registration.StartupName,
                registration.Sector
            },
            ct: ct);

        // KVKK onayı ayrı satır olarak yazılıyor — LoginHandler'daki kalıpla
        // aynı gerekçe: "kim, hangi metin sürümünü, ne zaman onayladı" sorusu
        // bağımsız süzülebilmeli. Aktör henüz yok (hesap onaydan sonra doğar),
        // bu yüzden izin hedefi başvurunun kendisi.
        if (!string.IsNullOrWhiteSpace(request.KvkkConsentVersion))
            await audit.WriteAsync(
                "Auth.KvkkConsent", nameof(StartupRegistrationRequest), registration.Id,
                after: new
                {
                    Email = MaskedEmail.Of(registration.Email),
                    ConsentVersion = request.KvkkConsentVersion.Trim(),
                },
                ct: ct);

        return new RegisterStartupResponse(registration.Id);
    }
}
