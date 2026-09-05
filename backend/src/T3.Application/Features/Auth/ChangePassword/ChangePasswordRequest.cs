using FluentValidation;
using T3.Application.Features.Users;

namespace T3.Application.Features.Auth.ChangePassword;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>
/// <see cref="AccessToken"/>/<see cref="ExpiresAt"/> yeni bir jetondur: şifre
/// değişince <c>SecurityStamp</c> yenilenir ve elindeki eski jetonu olan
/// (bu isteği yapan istemci dâhil) herkes bir sonraki istekte 401 alır. Aynı
/// oturumun kesintisiz sürmesi için uç bu jetonu hemen çereze yazıyor
/// (bkz. AuthEndpoints, SessionCookie.Issue) — kullanıcı kendi isteğiyle
/// şifresini değiştirdiğinde tekrar giriş yapmak zorunda kalmasın.
/// </summary>
public sealed record ChangePasswordResponse(DateTimeOffset ChangedAt, string AccessToken, DateTimeOffset ExpiresAt);

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Mevcut şifre zorunludur.")
            .MaximumLength(200).WithMessage("Mevcut şifre en fazla 200 karakter olabilir.");

        RuleFor(x => x.NewPassword).Password();

        RuleFor(x => x.NewPassword)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("Yeni şifre mevcut şifreden farklı olmalıdır.");
    }
}
