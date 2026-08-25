using FluentValidation;

namespace T3.Application.Features.Auth.Login;

public sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta zorunludur.")
            .MaximumLength(256).WithMessage("E-posta en fazla 256 karakter olabilir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre zorunludur.")
            .MaximumLength(200).WithMessage("Şifre en fazla 200 karakter olabilir.");

        // Onay sürümü serbest metin değil, ize yazılan kısa bir etiket: sınırsız
        // uzunlukta bir değer denetim izini istemcinin doldurabildiği bir alana çevirir.
        RuleFor(x => x.KvkkConsentVersion)
            .MaximumLength(32).WithMessage("Onay sürümü en fazla 32 karakter olabilir.")
            .When(x => x.KvkkConsentVersion is not null);
    }
}
