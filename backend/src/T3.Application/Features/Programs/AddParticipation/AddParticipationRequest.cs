using FluentValidation;
using T3.Domain.Programs;

namespace T3.Application.Features.Programs.AddParticipation;

public sealed record AddParticipationRequest(
    Guid StartupId,
    Guid ProgramTermId,
    ParticipationStatus Status,
    DateOnly JoinedOn,
    DateOnly? LeftOn,
    string? Notes);

public sealed record AddParticipationResponse(
    Guid Id,
    Guid StartupId,
    string StartupName,
    Guid ProgramId,
    string ProgramName,
    string TermName,
    ParticipationStatus Status,
    DateOnly JoinedOn,
    DateOnly? LeftOn);

public sealed class AddParticipationValidator : AbstractValidator<AddParticipationRequest>
{
    public AddParticipationValidator()
    {
        RuleFor(x => x.StartupId).NotEmpty().WithMessage("Girişim seçilmelidir.");
        RuleFor(x => x.ProgramTermId).NotEmpty().WithMessage("Program dönemi seçilmelidir.");
        RuleFor(x => x.Status).IsInEnum().WithMessage("Geçersiz katılım durumu.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Not en fazla 2000 karakter olabilir.");

        RuleFor(x => x.LeftOn)
            .GreaterThanOrEqualTo(x => x.JoinedOn)
            .WithMessage("Ayrılış tarihi katılım tarihinden önce olamaz.")
            .When(x => x.LeftOn is not null);
    }
}
