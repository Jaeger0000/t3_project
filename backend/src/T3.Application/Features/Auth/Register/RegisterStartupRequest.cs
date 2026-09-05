using FluentValidation;
using T3.Application.Features.Users;
using T3.Domain.Startups;

namespace T3.Application.Features.Auth.Register;

/// <summary>
/// Ana sayfadaki "Kayıt Ol" formunun gövdesi. Girişim kullanıcısı kendi
/// e-posta+şifresini burada seçer; hesap SuperAdmin onaylayana kadar hiçbir
/// tabloya yazılmaz — yalnızca <c>StartupRegistrationRequest</c> doğar.
/// </summary>
public sealed record RegisterStartupRequest(
    string Email,
    string Password,
    string FullName,
    string StartupName,
    Sector Sector,
    string? City,
    string? ContactPhone,
    string? KvkkConsentVersion = null);

public sealed class RegisterStartupValidator : AbstractValidator<RegisterStartupRequest>
{
    public RegisterStartupValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta zorunludur.")
            .MaximumLength(256).WithMessage("E-posta en fazla 256 karakter olabilir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Ad soyad zorunludur.")
            .MaximumLength(200).WithMessage("Ad soyad en fazla 200 karakter olabilir.");

        // Şifre kuralı kullanıcı yönetimindekiyle birebir aynı: admin eliyle
        // açılan hesapla kendi kendine açılan hesap farklı güvenlik seviyesinde
        // olamaz.
        RuleFor(x => x.Password).Password();

        RuleFor(x => x.StartupName)
            .NotEmpty().WithMessage("Girişim adı zorunludur.")
            .MaximumLength(200).WithMessage("Girişim adı en fazla 200 karakter olabilir.");

        RuleFor(x => x.Sector)
            .IsInEnum().WithMessage("Geçersiz sektör.");

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("Şehir en fazla 100 karakter olabilir.");

        RuleFor(x => x.ContactPhone)
            .MaximumLength(32).WithMessage("Telefon en fazla 32 karakter olabilir.");

        // Onay sürümü serbest metin değil, ize yazılan kısa bir etiket
        // (bkz. LoginValidator — aynı kural, aynı gerekçe).
        RuleFor(x => x.KvkkConsentVersion)
            .MaximumLength(32).WithMessage("Onay sürümü en fazla 32 karakter olabilir.")
            .When(x => x.KvkkConsentVersion is not null);
    }
}
