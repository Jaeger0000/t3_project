using FluentValidation;

namespace T3.Application.Features.Registrations.RejectRegistrationRequest;

/// <summary>
/// Ret gövdesi — <c>RejectChangeRequestRequest</c> ile birebir aynı kalıp.
/// Gerekçe zorunlu: reddedilen başvuru sahibi bir daha başvurmadan önce neyi
/// düzeltmesi gerektiğini bilmeli.
/// </summary>
public sealed record RejectRegistrationRequestRequest(string Note);

public sealed class RejectRegistrationRequestValidator
    : AbstractValidator<RejectRegistrationRequestRequest>
{
    public RejectRegistrationRequestValidator()
    {
        RuleFor(x => x.Note)
            .NotEmpty().WithMessage("Ret gerekçesi zorunludur.")
            .MinimumLength(10).WithMessage("Ret gerekçesi en az 10 karakter olmalıdır.")
            .MaximumLength(2000).WithMessage("Ret gerekçesi en fazla 2000 karakter olabilir.");
    }
}
