using FluentValidation;

namespace T3.Application.Features.Users.SetUserPassword;

/// <summary>
/// Yönetici tarafından şifre atama. Kullanıcı güncellemesinden ayrı uç:
/// profil düzenleme ile kimlik doğrulama sırrını değiştirme farklı ağırlıkta
/// işler, denetim izinde de ayrı görünmeleri gerekir.
/// </summary>
public sealed record SetUserPasswordRequest(string Password);

public sealed class SetUserPasswordValidator : AbstractValidator<SetUserPasswordRequest>
{
    public SetUserPasswordValidator()
    {
        RuleFor(x => x.Password).Password();
    }
}
