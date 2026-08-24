using FluentValidation;

namespace T3.Application.Features.Users;

/// <summary>
/// Şifre politikası. Oluşturma ve şifre sıfırlama aynı kuralı paylaşır —
/// birinin gevşek kalması, gevşek kapıdan girilen şifreyle diğer kapının
/// anlamsızlaşması demek.
///
/// Uzunluk karmaşıklığa tercih ediliyor: 10 karakter alt sınırı, harf ve
/// rakam zorunluluğu dışında özel karakter istenmiyor. Sert karmaşıklık
/// kuralları pratikte kâğıda yazılan şifreler üretir.
/// </summary>
internal static class PasswordRules
{
    public const int MinimumLength = 10;

    public static IRuleBuilderOptions<T, string> Password<T>(
        this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Şifre zorunludur.")
            .MinimumLength(MinimumLength)
                .WithMessage($"Şifre en az {MinimumLength} karakter olmalıdır.")
            .MaximumLength(128).WithMessage("Şifre en fazla 128 karakter olabilir.")
            .Matches("[A-Za-zçğıöşüÇĞİÖŞÜ]").WithMessage("Şifre en az bir harf içermelidir.")
            .Matches(@"\d").WithMessage("Şifre en az bir rakam içermelidir.");
}
