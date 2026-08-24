using FluentValidation;
using T3.Domain.Programs;

namespace T3.Application.Features.Programs.Terms;

/// <summary>Dönem ekleme ve güncellemenin ortak gövdesi.</summary>
public sealed record ProgramTermWriteModel(
    string Name,
    DateOnly StartsOn,
    DateOnly? EndsOn)
{
    public void ApplyTo(ProgramTerm term)
    {
        term.Name = Name.Trim();
        term.StartsOn = StartsOn;
        term.EndsOn = EndsOn;
    }
}

public sealed record ProgramTermSummaryResponse(
    Guid Id,
    Guid ProgramId,
    string ProgramName,
    string Name,
    DateOnly StartsOn,
    DateOnly? EndsOn,
    int ParticipantCount);

public sealed class ProgramTermWriteModelValidator : AbstractValidator<ProgramTermWriteModel>
{
    public ProgramTermWriteModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Dönem adı zorunludur.")
            .MaximumLength(200).WithMessage("Dönem adı en fazla 200 karakter olabilir.");

        RuleFor(x => x.StartsOn)
            .NotEqual(default(DateOnly)).WithMessage("Başlangıç tarihi zorunludur.");

        RuleFor(x => x.EndsOn)
            .GreaterThanOrEqualTo(x => x.StartsOn)
            .WithMessage("Bitiş tarihi başlangıçtan önce olamaz.")
            .When(x => x.EndsOn is not null);
    }
}
