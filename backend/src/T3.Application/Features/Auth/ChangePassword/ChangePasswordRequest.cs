using FluentValidation;
using T3.Application.Features.Users;

namespace T3.Application.Features.Auth.ChangePassword;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record ChangePasswordResponse(DateTimeOffset ChangedAt);

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
