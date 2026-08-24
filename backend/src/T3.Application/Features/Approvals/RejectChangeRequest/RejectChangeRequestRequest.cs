using FluentValidation;

namespace T3.Application.Features.Approvals.RejectChangeRequest;

/// <summary>
/// Ret gövdesi. Gerekçe zorunlu: girişim kullanıcısı önerisini neden
/// düzeltmesi gerektiğini portalda yalnızca bu nottan öğrenir. Gerekçesiz ret,
/// aynı önerinin tekrar gönderilmesiyle sonuçlanır.
/// </summary>
public sealed record RejectChangeRequestRequest(string Note);

public sealed class RejectChangeRequestValidator : AbstractValidator<RejectChangeRequestRequest>
{
    public RejectChangeRequestValidator()
    {
        RuleFor(x => x.Note)
            .NotEmpty().WithMessage("Ret gerekçesi zorunludur.")
            .MinimumLength(10).WithMessage("Ret gerekçesi en az 10 karakter olmalıdır.")
            .MaximumLength(2000).WithMessage("Ret gerekçesi en fazla 2000 karakter olabilir.");
    }
}
