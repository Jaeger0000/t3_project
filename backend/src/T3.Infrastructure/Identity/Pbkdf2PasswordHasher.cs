using Microsoft.AspNetCore.Identity;
using T3.Application.Common.Interfaces;
using T3.Domain.Identity;

namespace T3.Infrastructure.Identity;

/// <summary>
/// ASP.NET Core'un PBKDF2 tabanlı hash'leyicisini sarar. Düz metin şifre
/// hiçbir yerde saklanmaz veya loglanmaz.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _inner = new();
    private static readonly User Dummy = new();

    public string Hash(string password) => _inner.HashPassword(Dummy, password);

    public bool Verify(string password, string hash) =>
        _inner.VerifyHashedPassword(Dummy, hash, password) is
            PasswordVerificationResult.Success or
            PasswordVerificationResult.SuccessRehashNeeded;
}
