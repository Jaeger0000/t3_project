using FluentValidation;
using T3.Domain.Startups;

namespace T3.Application.Features.Startups.Team;

/// <summary>Ekip üyesi ekleme/güncelleme gövdesi. Tüm alanları kişisel veridir.</summary>
public sealed record TeamMemberWriteModel(
    string FullName,
    string? Title,
    string? Email,
    string? Phone,
    string? LinkedInUrl,
    bool IsFounder,
    DateOnly? JoinedOn);

public sealed class TeamMemberWriteModelValidator : AbstractValidator<TeamMemberWriteModel>
{
    public TeamMemberWriteModelValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Ad soyad zorunludur.")
            .MaximumLength(200).WithMessage("Ad soyad en fazla 200 karakter olabilir.");

        RuleFor(x => x.Title)
            .MaximumLength(120).WithMessage("Ünvan en fazla 120 karakter olabilir.");

        RuleFor(x => x.Email)
            .MaximumLength(256).WithMessage("E-posta en fazla 256 karakter olabilir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone)
            .MaximumLength(32).WithMessage("Telefon en fazla 32 karakter olabilir.");

        RuleFor(x => x.LinkedInUrl)
            .MaximumLength(300).WithMessage("LinkedIn bağlantısı en fazla 300 karakter olabilir.");

        RuleFor(x => x.JoinedOn)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Katılım tarihi gelecekte olamaz.")
            .When(x => x.JoinedOn is not null);
    }
}

public sealed record TeamMemberResponse(
    Guid Id,
    Guid StartupId,
    string FullName,
    string? Title,
    string? Email,
    string? Phone,
    string? LinkedInUrl,
    bool IsFounder,
    DateOnly? JoinedOn);

internal static class TeamMemberWriteModelExtensions
{
    public static void ApplyTo(this TeamMemberWriteModel model, TeamMember member)
    {
        member.FullName = model.FullName.Trim();
        member.Title = Clean(model.Title);
        member.Email = Clean(model.Email)?.ToLowerInvariant();
        member.Phone = Clean(model.Phone);
        member.LinkedInUrl = Clean(model.LinkedInUrl);
        member.IsFounder = model.IsFounder;
        member.JoinedOn = model.JoinedOn;
    }

    public static TeamMemberResponse ToResponse(this TeamMember member) => new(
        member.Id, member.StartupId, member.FullName, member.Title,
        member.Email, member.Phone, member.LinkedInUrl,
        member.IsFounder, member.JoinedOn);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
