using FluentValidation;

namespace T3.Application.Features.Approvals.ApproveChangeRequest;

/// <summary>
/// Onay gövdesi. Not isteğe bağlı: onaylarken açıklama zorunlu tutmak, gerçek
/// kullanımda "ok" gibi anlamsız notlar üretir. Redde ise zorunludur —
/// gerekçesiz ret girişimin ne yapacağını bilmesini engeller.
/// </summary>
public sealed record ApproveChangeRequestRequest(string? Note);

public sealed class ApproveChangeRequestValidator : AbstractValidator<ApproveChangeRequestRequest>
{
    public ApproveChangeRequestValidator()
    {
        RuleFor(x => x.Note)
            .MaximumLength(2000).WithMessage("Not en fazla 2000 karakter olabilir.");
    }
}
