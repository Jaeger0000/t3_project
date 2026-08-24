using FluentValidation;
using T3.Application.Features.Users;

namespace T3.Application.Features.Auth.ResetPassword;

public sealed record ResetPasswordRequest(string Token, string NewPassword);

public sealed record ResetPasswordResponse(string Email, DateTimeOffset ChangedAt);

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Sıfırlama bağlantısı eksik.")
            .MaximumLength(200).WithMessage("Sıfırlama bağlantısı geçersiz.");

        // Şifre politikası yönetici atamasıyla aynı: iki kapıdan biri gevşek
        // kalırsa diğerinin sıkı olması anlamsız.
        RuleFor(x => x.NewPassword).Password();
    }
}
