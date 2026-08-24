using FluentValidation;
using T3.Domain.Identity;

namespace T3.Application.Features.Users.CreateUser;

/// <summary>
/// Yeni kullanıcı gövdesi. Oluşturma ve güncelleme ortak şekil kullanmıyor:
/// e-posta ve şifre yalnızca oluşturmada yazılıyor, şifre değişikliği ayrı
/// uçtan geçiyor. Tek modelde birleştirmek "güncellemede şifre gönderilirse ne
/// olur" sorusunu doğururdu.
/// </summary>
public sealed record CreateUserRequest(
    string Email,
    string FullName,
    UserRole Role,
    string Password,
    Guid? StartupId,
    IReadOnlyList<Guid>? ProgramIds);

public sealed class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta zorunludur.")
            .MaximumLength(256).WithMessage("E-posta en fazla 256 karakter olabilir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Ad soyad zorunludur.")
            .MaximumLength(200).WithMessage("Ad soyad en fazla 200 karakter olabilir.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Geçersiz rol.");

        RuleFor(x => x.Password).Password();

        // Rol ile kapsam bağı tek yerde tanımlı; doğrulayıcı da handler da aynı
        // işlevi çağırıyor.
        RuleFor(x => x)
            .Must(x => UserAdminGuard.ValidateBinding(x.Role, x.StartupId, x.ProgramIds) is null)
            .WithMessage(x =>
                UserAdminGuard.ValidateBinding(x.Role, x.StartupId, x.ProgramIds)?.Message
                ?? "Rol ile atama uyumsuz.")
            .When(x => Enum.IsDefined(x.Role));
    }
}
