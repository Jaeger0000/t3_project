using FluentValidation;
using T3.Domain.Identity;

namespace T3.Application.Features.Users.UpdateUser;

/// <summary>
/// Kullanıcı güncelleme gövdesi. E-posta değiştirilemez: e-posta kimlik
/// belirteci ve denetim izinde aktörün insan tarafından okunur karşılığı;
/// taşınması izin geçmişini bulanıklaştırır. Şifre de burada değil, ayrı uçta.
/// </summary>
public sealed record UpdateUserRequest(
    string FullName,
    UserRole Role,
    Guid? StartupId,
    IReadOnlyList<Guid>? ProgramIds,
    bool IsActive);

public sealed class UpdateUserValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Ad soyad zorunludur.")
            .MaximumLength(200).WithMessage("Ad soyad en fazla 200 karakter olabilir.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Geçersiz rol.");

        RuleFor(x => x)
            .Must(x => UserAdminGuard.ValidateBinding(x.Role, x.StartupId, x.ProgramIds) is null)
            .WithMessage(x =>
                UserAdminGuard.ValidateBinding(x.Role, x.StartupId, x.ProgramIds)?.Message
                ?? "Rol ile atama uyumsuz.")
            .When(x => Enum.IsDefined(x.Role));
    }
}
