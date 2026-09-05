using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Application.Common.Text;
using T3.Domain.Identity;

namespace T3.Application.Features.Auth.Login;

public sealed class LoginHandler(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IAuditWriter audit)
{
    /// <summary>
    /// Kimlik doğrulama hatalarında e-postanın kayıtlı olup olmadığı bilgisi
    /// sızdırılmaz: yanlış şifre, olmayan kullanıcı ve pasif hesap aynı mesajı
    /// döner. Böylece giriş ekranı kullanıcı adı numaralandırmaya hizmet etmez.
    ///
    /// Ayrım yalnızca denetim izine yazılıyor: "hangi hesap kilitleniyor" ve
    /// "var olmayan adresler mi taranıyor" güvenlik incelemesinin iki farklı
    /// sorusu ve ize yalnızca sistem yöneticisi erişiyor.
    /// </summary>
    private static readonly Error InvalidCredentials =
        Error.Validation("E-posta veya şifre hatalı.");

    /// <summary>
    /// Hız sınırı (IP+e-posta, dakikada 10) tek bir kaynaktan gelen art arda
    /// denemeyi zaten engelliyor; bu sayaç zamana ya da IP'ye yayılmış
    /// denemelere karşı ikinci bir katman (bkz. G-05).
    /// </summary>
    private const int MaxFailedAttempts = 10;

    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<Result<LoginResponse>> Handle(LoginRequest request, CancellationToken ct)
    {
        var email = SearchText.Normalize(request.Email);

        var user = await db.Users
            .Include(u => u.Startup)
            .Include(u => u.ProgramAssignments)
                .ThenInclude(a => a.Program)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email, ct);

        if (user is null)
        {
            // Kayıtlı olmayan adres de bir PBKDF2 doğrulaması "harcar": aksi
            // hâlde bu yol diğerlerinden onlarca milisaniye hızlı yanıt verir
            // ve zamanlama kayıtlı adresi ele verir (bkz. G-05).
            passwordHasher.Verify(request.Password, passwordHasher.DummyHashForTiming);
            return await FailAsync(request.Email, "kayıtlı olmayan adres", null, ct);
        }

        if (!user.IsActive)
        {
            passwordHasher.Verify(request.Password, passwordHasher.DummyHashForTiming);
            return await FailAsync(request.Email, "hesap pasif", user, ct);
        }

        if (user.LockedUntil is { } lockedUntil && lockedUntil > DateTimeOffset.UtcNow)
        {
            passwordHasher.Verify(request.Password, passwordHasher.DummyHashForTiming);
            return await FailAsync(request.Email, "hesap kilitli", user, ct);
        }

        var verified = passwordHasher.VerifyAndGetRehash(
            request.Password, user.PasswordHash, out var rehashedHash);

        if (!verified)
        {
            user.FailedLoginCount++;

            var reason = "şifre hatalı";

            if (user.FailedLoginCount >= MaxFailedAttempts)
            {
                user.LockedUntil = DateTimeOffset.UtcNow.Add(LockoutDuration);
                user.FailedLoginCount = 0;
                reason = "hesap kilitlendi";
            }

            // Sayaç/kilit değişikliği ile "Auth.LoginFailed" izi FailAsync
            // içinde TEK SaveChanges'ta birlikte kalıcı olur (bkz. G-09).
            return await FailAsync(request.Email, reason, user, ct);
        }

        user.FailedLoginCount = 0;
        user.LockedUntil = null;

        // Hash eski parametrelerle (ör. düşük iterasyon) üretilmişse burada
        // sessizce yükseltilir — kullanıcı doğru şifreyi zaten girdi, düz
        // metin yalnızca bu isteğin belleğinde var (bkz. G-15).
        if (rehashedHash is not null)
            user.PasswordHash = rehashedHash;

        var programs = user.ProgramAssignments
            .Select(a => new AssignedProgramResponse(a.ProgramId, a.Program.Name))
            .OrderBy(p => p.Name)
            .ToArray();

        var token = tokenService.CreateAccessToken(
            user, programs.Select(p => p.Id).ToArray());

        user.LastLoginAt = DateTimeOffset.UtcNow;

        // Aktör açıkça veriliyor: jeton bu isteğin içinde üretildi, henüz hiçbir
        // ardışık düzen bileşeni kullanıcıyı tanımıyor. saveChanges: false —
        // kullanıcı satırındaki değişiklikler ve (varsa) KVKK onay izi ile
        // birlikte tek SaveChanges'ta kalıcı olacak (bkz. G-09).
        await audit.WriteForActorAsync(
            user.Id, user.Role,
            "Auth.LoginSucceeded", nameof(User), user.Id,
            after: new { Email = MaskedEmail.Of(user.Email), Role = user.Role.ToString() },
            ct: ct, saveChanges: false);

        // KVKK onayı ayrı satır olarak yazılıyor: "kim, hangi metin sürümünü, ne
        // zaman onayladı" sorusu giriş olaylarından bağımsız süzülebilmeli
        // (denetim ekranındaki "KVKK onayı" süzgeci bu adı arıyor).
        if (!string.IsNullOrWhiteSpace(request.KvkkConsentVersion))
            await audit.WriteForActorAsync(
                user.Id, user.Role,
                "Auth.KvkkConsent", nameof(User), user.Id,
                after: new
                {
                    Email = MaskedEmail.Of(user.Email),
                    ConsentVersion = request.KvkkConsentVersion.Trim(),
                },
                ct: ct, saveChanges: false);

        await db.SaveChangesAsync(ct);

        return new LoginResponse(
            token.Value,
            token.ExpiresAt,
            SessionUserMapper.Map(user, programs));
    }

    /// <summary>
    /// Başarısız denemeyi ize yazar ve her durumda aynı hatayı döner.
    /// E-posta maskelenerek saklanıyor — iz, denenen adreslerin ham listesine
    /// dönüşmemeli. Tek SaveChanges: bazı çağıranlarda (yanlış şifre) sayaç/kilit
    /// değişikliği de burada aynı işlemle kalıcı olur (bkz. G-09).
    /// </summary>
    private async Task<Result<LoginResponse>> FailAsync(
        string attemptedEmail, string reason, User? user, CancellationToken ct)
    {
        await audit.WriteForActorAsync(
            user?.Id, user?.Role,
            "Auth.LoginFailed", nameof(User), user?.Id,
            after: new { Email = MaskedEmail.Of(attemptedEmail), Reason = reason },
            ct: ct, saveChanges: false);

        await db.SaveChangesAsync(ct);

        return InvalidCredentials;
    }
}
