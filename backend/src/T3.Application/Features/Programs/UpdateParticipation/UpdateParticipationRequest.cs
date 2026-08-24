using FluentValidation;
using T3.Domain.Programs;

namespace T3.Application.Features.Programs.UpdateParticipation;

/// <summary>
/// Katılım düzeltmesi. Girişim ve dönem bilinçli olarak <b>yok</b>: yanlış
/// döneme eklenen kayıt taşınmıyor, kaldırılıp yeniden ekleniyor — taşımak
/// gelişim yolculuğundaki tarihi tek istekle yeniden yazmak olurdu.
/// </summary>
public sealed record UpdateParticipationRequest(
    ParticipationStatus Status,
    DateOnly JoinedOn,
    DateOnly? LeftOn,
    string? Notes);

public sealed record UpdateParticipationResponse(
    Guid Id,
    Guid StartupId,
    string StartupName,
    Guid ProgramId,
    string ProgramName,
    string TermName,
    ParticipationStatus Status,
    DateOnly JoinedOn,
    DateOnly? LeftOn,
    string? Notes);

public sealed class UpdateParticipationValidator : AbstractValidator<UpdateParticipationRequest>
{
    public UpdateParticipationValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithMessage("Geçersiz katılım durumu.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Not en fazla 2000 karakter olabilir.");

        RuleFor(x => x.LeftOn)
            .GreaterThanOrEqualTo(x => x.JoinedOn)
            .WithMessage("Ayrılış tarihi katılım tarihinden önce olamaz.")
            .When(x => x.LeftOn is not null);
    }
}
