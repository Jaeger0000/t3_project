using FluentValidation;

namespace T3.Application.Features.Auth.RequestPasswordReset;

public sealed record RequestPasswordResetRequest(string Email);

/// <summary>
/// Yanıt bilinçli olarak içeriksiz: e-posta kayıtlı olsun olmasın aynı gövde
/// döner. Aksi hâlde uç, kimlerin sistemde olduğunu sorgulayan bir
/// numaralandırma aracına dönüşür.
/// </summary>
public sealed record RequestPasswordResetResponse(string Message);

public sealed class RequestPasswordResetValidator : AbstractValidator<RequestPasswordResetRequest>
{
    public RequestPasswordResetValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta zorunludur.")
            .MaximumLength(256).WithMessage("E-posta en fazla 256 karakter olabilir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.");
    }
}
