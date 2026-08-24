using FluentValidation;
using T3.Application.Common.Rbac;
using T3.Domain.Startups;

namespace T3.Application.Features.Startups;

/// <summary>
/// Girişim oluşturma ve güncellemenin ortak gövdesi. İki dilim aynı alan
/// kümesini yazdığı için (PUT tam değiştirme) tek şekil kullanılıyor; ayrı
/// tutmak iki doğrulayıcıyı senkron tutma yükü getirirdi.
/// </summary>
public sealed record StartupWriteModel(
    string Name,
    string? LegalName,
    string? TaxNumber,
    DateOnly? FoundedOn,
    Sector Sector,
    IReadOnlyList<string>? TechnologyAreas,
    string? ProductDescription,
    string? Website,
    string? LogoUrl,
    string? City,
    string? ContactEmail,
    string? ContactPhone,
    StartupStatus? Status)
{
    /// <summary>
    /// Varlığın mevcut hâlini öneri gövdesiyle aynı şekle çevirir. Onay
    /// akışının "önce" anlık görüntüsü bu biçimde saklanır: iki taraf aynı
    /// alan kümesini taşıdığı için diff alanları birebir hizalanabiliyor.
    /// </summary>
    public static StartupWriteModel From(Startup s) => new(
        s.Name, s.LegalName, s.TaxNumber, s.FoundedOn, s.Sector,
        [.. s.TechnologyAreas], s.ProductDescription, s.Website, s.LogoUrl,
        s.City, s.ContactEmail, s.ContactPhone, s.Status);

    /// <summary>
    /// Modeli varlığa uygular ama <b>kullanıcının göremediği alanı yazmaz</b>.
    ///
    /// Maskeli alan istemciye <c>null</c> gittiği için düz uygulama onu sessizce
    /// <em>siliyordu</em>: vergi numarasını göremeyen Program Yöneticisi'nin
    /// kaydettiği her düzenleme numarayı boşaltırdı. Maskeleme yalnızca okumayı
    /// daraltan bir süzgeç değil; yazma yolunda da tutulmak zorunda.
    /// </summary>
    public void ApplyTo(Startup startup, StartupVisibility visibility)
    {
        var taxNumber = startup.TaxNumber;
        var contactEmail = startup.ContactEmail;
        var contactPhone = startup.ContactPhone;

        this.ApplyTo(startup);

        if (!visibility.ShowTaxNumber)
            startup.TaxNumber = taxNumber;

        if (!visibility.ShowContactDetails)
        {
            startup.ContactEmail = contactEmail;
            startup.ContactPhone = contactPhone;
        }
    }
}

/// <summary>
/// Alan kısıtları EF yapılandırmasındaki kolon uzunluklarıyla birebir aynı
/// tutulur; aksi halde doğrulamayı geçen veri veritabanında patlar.
/// </summary>
public sealed class StartupWriteModelValidator : AbstractValidator<StartupWriteModel>
{
    public StartupWriteModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Girişim adı zorunludur.")
            .MaximumLength(200).WithMessage("Girişim adı en fazla 200 karakter olabilir.");

        RuleFor(x => x.LegalName)
            .MaximumLength(300).WithMessage("Ticari unvan en fazla 300 karakter olabilir.");

        RuleFor(x => x.TaxNumber)
            .Matches(@"^\d{10,11}$")
            .WithMessage("Vergi kimlik numarası 10 veya 11 haneli olmalıdır.")
            .When(x => !string.IsNullOrWhiteSpace(x.TaxNumber));

        RuleFor(x => x.Sector)
            .IsInEnum().WithMessage("Geçersiz sektör.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Geçersiz durum.")
            .When(x => x.Status is not null);

        RuleFor(x => x.FoundedOn)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Kuruluş tarihi gelecekte olamaz.")
            .When(x => x.FoundedOn is not null);

        RuleFor(x => x.ProductDescription)
            .MaximumLength(4000).WithMessage("Ürün açıklaması en fazla 4000 karakter olabilir.");

        RuleFor(x => x.Website)
            .MaximumLength(300).WithMessage("Web sitesi en fazla 300 karakter olabilir.");

        RuleFor(x => x.LogoUrl)
            .MaximumLength(500).WithMessage("Logo bağlantısı en fazla 500 karakter olabilir.");

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("Şehir en fazla 100 karakter olabilir.");

        RuleFor(x => x.ContactEmail)
            .MaximumLength(256).WithMessage("E-posta en fazla 256 karakter olabilir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.")
            .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));

        RuleFor(x => x.ContactPhone)
            .MaximumLength(32).WithMessage("Telefon en fazla 32 karakter olabilir.");

        RuleFor(x => x.TechnologyAreas)
            .Must(areas => areas is null || areas.Count <= 20)
            .WithMessage("En fazla 20 teknoloji alanı eklenebilir.");

        RuleForEach(x => x.TechnologyAreas)
            .NotEmpty().WithMessage("Teknoloji alanı boş olamaz.")
            .MaximumLength(100).WithMessage("Teknoloji alanı en fazla 100 karakter olabilir.");
    }
}

internal static class StartupWriteModelExtensions
{
    /// <summary>
    /// Modeli varlığa uygular. Boş metinler <c>null</c>'a çevrilir: veritabanında
    /// "" ile null arasında ayrım tutmak sorguları ve maskeleme mantığını
    /// gereksiz karmaşıklaştırır.
    /// </summary>
    public static void ApplyTo(this StartupWriteModel model, Startup startup)
    {
        startup.Name = model.Name.Trim();
        startup.LegalName = Clean(model.LegalName);
        startup.TaxNumber = Clean(model.TaxNumber);
        startup.FoundedOn = model.FoundedOn;
        startup.Sector = model.Sector;
        startup.TechnologyAreas = model.TechnologyAreas?
            .Select(a => a.Trim())
            .Where(a => a.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
        startup.ProductDescription = Clean(model.ProductDescription);
        startup.Website = Clean(model.Website);
        startup.LogoUrl = Clean(model.LogoUrl);
        startup.City = Clean(model.City);
        startup.ContactEmail = Clean(model.ContactEmail)?.ToLowerInvariant();
        startup.ContactPhone = Clean(model.ContactPhone);
        startup.Status = model.Status ?? startup.Status;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
