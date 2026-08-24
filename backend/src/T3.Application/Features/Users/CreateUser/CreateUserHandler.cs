using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Domain.Identity;

namespace T3.Application.Features.Users.CreateUser;

/// <summary>
/// Yeni hesap açar. Denetim izine şifreye dair hiçbir şey yazılmaz — ne düz
/// metin ne özet. İz "hesap açıldı" bilgisini taşımalı, kimlik doğrulama
/// sırrının kopyasını değil.
/// </summary>
public sealed class CreateUserHandler(
    IAppDbContext db,
    UserAdminGuard guard,
    IPasswordHasher passwordHasher,
    IAuditWriter audit)
{
    public async Task<Result<UserResponse>> Handle(CreateUserRequest request, CancellationToken ct)
    {
        if (guard.EnsureCanManage() is { } denied)
            return denied;

        if (UserAdminGuard.ValidateBinding(request.Role, request.StartupId, request.ProgramIds)
            is { } binding)
            return binding;

        if (await guard.EnsureEmailAvailableAsync(request.Email, null, ct) is { } emailTaken)
            return emailTaken;

        if (await guard.EnsureStartupExistsAsync(request.StartupId, ct) is { } noStartup)
            return noStartup;

        var user = new User
        {
            // E-posta küçük harfe indiriliyor: giriş akışı da normalleştirilmiş
            // karşılaştırma yapıyor, iki taraf aynı biçimi görmeli.
            Email = request.Email.Trim().ToLowerInvariant(),
            FullName = request.FullName.Trim(),
            Role = request.Role,
            StartupId = request.StartupId,
            PasswordHash = passwordHasher.Hash(request.Password),
            IsActive = true,

            // Yöneticinin belirlediği ilk şifre geçici: hesabı devralan kişi
            // ilk girişte kendi şifresini koymadan başka ekrana geçemez.
            MustChangePassword = true
        };

        db.Users.Add(user);

        if (await guard.SyncProgramsAsync(user, request.ProgramIds, ct) is { } noProgram)
            return noProgram;

        await db.SaveChangesAsync(ct);

        await audit.WriteAsync(
            "User.Create", nameof(User), user.Id,
            after: new
            {
                user.Email,
                user.FullName,
                user.Role,
                user.StartupId,
                ProgramIds = request.ProgramIds ?? []
            },
            ct: ct);

        return await guard.LoadResponseAsync(user.Id, ct);
    }
}
