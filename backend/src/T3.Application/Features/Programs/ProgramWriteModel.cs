using FluentValidation;
using T3.Domain.Programs;

namespace T3.Application.Features.Programs;

/// <summary>
/// Program oluşturma ve güncellemenin ortak gövdesi — ikisi aynı alan kümesini
/// yazıyor (PUT tam değiştirme), ayrı tutmak iki doğrulayıcıyı senkron tutma
/// yükü getirirdi.
/// </summary>
public sealed record ProgramWriteModel(
    string Name,
    ProgramType Type,
    string? Coordinatorship,
    string? Description)
{
    public void ApplyTo(EcosystemProgram program)
    {
        program.Name = Name.Trim();
        program.Type = Type;
        program.Coordinatorship = Clean(Coordinatorship);
        program.Description = Clean(Description);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>
/// Program özeti. Liste yanıtı (katılım sayıları, dönemler) yazma
/// işlemlerinden dönmüyor: arayüz yazma sonrası listeyi tazeliyor, sayıları
/// burada tekrar hesaplamak aynı veriyi iki yoldan üretmek olurdu.
/// </summary>
public sealed record ProgramSummaryResponse(
    Guid Id,
    string Name,
    ProgramType Type,
    string? Coordinatorship,
    string? Description);

/// <summary>
/// Alan kısıtları EF yapılandırmasındaki kolon uzunluklarıyla birebir aynı.
/// </summary>
public sealed class ProgramWriteModelValidator : AbstractValidator<ProgramWriteModel>
{
    public ProgramWriteModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Program adı zorunludur.")
            .MaximumLength(200).WithMessage("Program adı en fazla 200 karakter olabilir.");

        RuleFor(x => x.Type).IsInEnum().WithMessage("Geçersiz program türü.");

        RuleFor(x => x.Coordinatorship)
            .MaximumLength(200).WithMessage("Koordinatörlük en fazla 200 karakter olabilir.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Açıklama en fazla 2000 karakter olabilir.");
    }
}
