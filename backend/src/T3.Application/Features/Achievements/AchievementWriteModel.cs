using FluentValidation;
using T3.Application.Features.Startups;
using T3.Domain.Achievements;

namespace T3.Application.Features.Achievements;

/// <summary>
/// Başarı/finans kaydının türü (MVP #4).
///
/// Domain tarafında beş ayrı sınıf var (TPH); dış dünyaya tek gövde açılıyor
/// çünkü form da tek: kullanıcı önce türü seçer, sonra o türün alanlarını
/// doldurur. Tür başına ayrı uç nokta açmak beş kez aynı yetki ve onay
/// mantığını kopyalamak demekti.
/// </summary>
public enum AchievementKind
{
    Revenue = 1,
    Export = 2,
    Investment = 3,
    Grant = 4,
    Award = 5
}

/// <summary>
/// Başarı kaydı ekleme/güncelleme gövdesi. Tür dışındaki alanlar isteğe bağlı
/// görünür ama doğrulayıcı türe göre zorunlu kılar — brief "serbest metin değil,
/// alan bazlı veri modeli" istiyor, dolayısıyla bir yatırım turu tutarsız
/// kaydedilemez.
/// </summary>
public sealed record AchievementWriteModel(
    AchievementKind Kind,
    DateOnly OccurredOn,
    string? Note,

    // Tutar taşıyan türler (Ciro, İhracat, Yatırım, Hibe)
    decimal? Amount,
    string? Currency,

    // Dönemsel türler (Ciro, İhracat)
    int? FiscalYear,
    int? Quarter,

    // Yatırım turu
    InvestmentRoundType? RoundType,
    decimal? Valuation,
    IReadOnlyList<string>? InvestorNames,

    // Hibe / destek
    GrantInstitution? Institution,
    string? ProgramName,

    // Ödül
    string? AwardName,
    string? Organization,
    int? Rank,

    // İhracat
    IReadOnlyList<string>? TargetCountries)
{
    /// <summary>Onay akışının "önce" anlık görüntüsü için mevcut kaydı aynı şekle çevirir.</summary>
    public static AchievementWriteModel From(Achievement a) => new(
        Kind: AchievementKinds.Of(a),
        OccurredOn: a.OccurredOn,
        Note: a.Note,
        Amount: (a as MoneyAchievement)?.Amount,
        Currency: (a as MoneyAchievement)?.Currency,
        FiscalYear: (a as PeriodicMoneyAchievement)?.FiscalYear,
        Quarter: (a as PeriodicMoneyAchievement)?.Quarter,
        RoundType: (a as InvestmentRound)?.RoundType,
        Valuation: (a as InvestmentRound)?.Valuation,
        InvestorNames: (a as InvestmentRound)?.InvestorNames,
        Institution: (a as GrantRecord)?.Institution,
        ProgramName: (a as GrantRecord)?.ProgramName,
        AwardName: (a as AwardRecord)?.Name,
        Organization: (a as AwardRecord)?.Organization,
        Rank: (a as AwardRecord)?.Rank,
        TargetCountries: (a as ExportRecord)?.TargetCountries);
}

/// <summary>
/// Tür ile alanlar arasındaki bağı zorlar. Doğrulama burada tek noktada durur;
/// hem doğrudan yazma yolu hem onay akışı aynı kuralları geçmek zorunda,
/// aksi hâlde onay, doğrulamayı atlamanın yolu olurdu.
/// </summary>
public sealed class AchievementWriteModelValidator : AbstractValidator<AchievementWriteModel>
{
    /// <summary>Tutar alanı olan türler.</summary>
    private static bool HasMoney(AchievementKind kind) =>
        kind is AchievementKind.Revenue or AchievementKind.Export
            or AchievementKind.Investment or AchievementKind.Grant;

    /// <summary>Mali yıl/çeyrek alanı olan türler.</summary>
    private static bool HasPeriod(AchievementKind kind) =>
        kind is AchievementKind.Revenue or AchievementKind.Export;

    public AchievementWriteModelValidator()
    {
        RuleFor(x => x.Kind).IsInEnum().WithMessage("Geçersiz kayıt türü.");

        RuleFor(x => x.OccurredOn)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Kayıt tarihi gelecekte olamaz.")
            .GreaterThanOrEqualTo(new DateOnly(1990, 1, 1))
            .WithMessage("Kayıt tarihi 1990'dan önce olamaz.");

        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage("Not en fazla 1000 karakter olabilir.");

        When(x => HasMoney(x.Kind), () =>
        {
            RuleFor(x => x.Amount)
                .NotNull().WithMessage("Tutar zorunludur.")
                .GreaterThan(0).WithMessage("Tutar sıfırdan büyük olmalıdır.")
                .LessThanOrEqualTo(1_000_000_000_000m)
                .WithMessage("Tutar beklenenden büyük; birim hatası olabilir.");

            // Para birimi serbest metin değil: agregatlar yalnızca aynı birimi
            // toplayabiliyor, "TL" ile "TRY" karışırsa toplam sessizce bozulur.
            RuleFor(x => x.Currency)
                .Matches("^[A-Za-z]{3}$")
                .WithMessage("Para birimi üç harfli ISO kodu olmalıdır (TRY, USD, EUR).")
                .When(x => !string.IsNullOrWhiteSpace(x.Currency));
        });

        When(x => HasPeriod(x.Kind), () =>
        {
            RuleFor(x => x.FiscalYear)
                .NotNull().WithMessage("Mali yıl zorunludur.")
                .InclusiveBetween(1990, DateTime.UtcNow.Year + 1)
                .WithMessage("Mali yıl geçerli aralıkta olmalıdır.");

            RuleFor(x => x.Quarter)
                .InclusiveBetween(1, 4).WithMessage("Çeyrek 1 ile 4 arasında olmalıdır.")
                .When(x => x.Quarter is not null);
        });

        When(x => x.Kind == AchievementKind.Investment, () =>
        {
            RuleFor(x => x.RoundType)
                .NotNull().WithMessage("Yatırım turu tipi zorunludur.")
                .IsInEnum().WithMessage("Geçersiz yatırım turu tipi.");

            RuleFor(x => x.Valuation)
                .GreaterThan(0).WithMessage("Değerleme sıfırdan büyük olmalıdır.")
                .When(x => x.Valuation is not null);

            RuleFor(x => x.InvestorNames)
                .Must(names => names is null || names.Count <= 20)
                .WithMessage("En fazla 20 yatırımcı listelenebilir.")
                .Must(names => names is null || names.All(n => !string.IsNullOrWhiteSpace(n) && n.Length <= 200))
                .WithMessage("Yatırımcı adı boş olamaz ve 200 karakteri aşamaz.");
        });

        When(x => x.Kind == AchievementKind.Grant, () =>
        {
            RuleFor(x => x.Institution)
                .NotNull().WithMessage("Destek veren kurum zorunludur.")
                .IsInEnum().WithMessage("Geçersiz kurum.");

            RuleFor(x => x.ProgramName)
                .MaximumLength(300).WithMessage("Program adı en fazla 300 karakter olabilir.");
        });

        When(x => x.Kind == AchievementKind.Award, () =>
        {
            RuleFor(x => x.AwardName)
                .NotEmpty().WithMessage("Ödül adı zorunludur.")
                .MaximumLength(300).WithMessage("Ödül adı en fazla 300 karakter olabilir.");

            RuleFor(x => x.Organization)
                .MaximumLength(300).WithMessage("Kurum adı en fazla 300 karakter olabilir.");

            RuleFor(x => x.Rank)
                .InclusiveBetween(1, 100).WithMessage("Derece 1 ile 100 arasında olmalıdır.")
                .When(x => x.Rank is not null);
        });

        When(x => x.Kind == AchievementKind.Export, () =>
        {
            RuleFor(x => x.TargetCountries)
                .Must(c => c is null || c.Count <= 30)
                .WithMessage("En fazla 30 ülke listelenebilir.")
                .Must(c => c is null || c.All(v => !string.IsNullOrWhiteSpace(v) && v.Length <= 100))
                .WithMessage("Ülke adı boş olamaz ve 100 karakteri aşamaz.");
        });
    }
}

/// <summary>
/// Gövde ile TPH sınıfları arasındaki eşleme. Elle yazıldı: hangi alanın hangi
/// türde anlamlı olduğu okunabilir kalsın — otomatik eşleyici burada "ödülün
/// mali yılı" gibi anlamsız alanları sessizce taşırdı.
/// </summary>
internal static class AchievementKinds
{
    public static AchievementKind Of(Achievement a) => a switch
    {
        RevenueRecord => AchievementKind.Revenue,
        ExportRecord => AchievementKind.Export,
        InvestmentRound => AchievementKind.Investment,
        GrantRecord => AchievementKind.Grant,
        AwardRecord => AchievementKind.Award,
        _ => throw new InvalidOperationException($"Bilinmeyen başarı kaydı türü: {a.GetType().Name}")
    };

    /// <summary>Türe uygun boş varlık üretir; alanlar <see cref="ApplyTo"/> ile doldurulur.</summary>
    public static Achievement NewFor(AchievementKind kind, Guid startupId)
    {
        Achievement entity = kind switch
        {
            AchievementKind.Revenue => new RevenueRecord(),
            AchievementKind.Export => new ExportRecord(),
            AchievementKind.Investment => new InvestmentRound(),
            AchievementKind.Grant => new GrantRecord(),
            AchievementKind.Award => new AwardRecord(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        entity.StartupId = startupId;
        return entity;
    }

    public static void ApplyTo(this AchievementWriteModel model, Achievement entity)
    {
        entity.OccurredOn = model.OccurredOn;
        entity.Note = Clean(model.Note);

        if (entity is MoneyAchievement money)
        {
            money.Amount = model.Amount ?? 0m;

            // Birim verilmediyse rapor birimi varsayılıyor: agregatların
            // dayandığı tek birim bu, boş bırakılan kayıt toplamdan düşerdi.
            money.Currency = (Clean(model.Currency) ?? StartupMoney.ReportingCurrency)
                .ToUpperInvariant();
        }

        if (entity is PeriodicMoneyAchievement periodic)
        {
            periodic.FiscalYear = model.FiscalYear ?? model.OccurredOn.Year;
            periodic.Quarter = model.Quarter;
        }

        switch (entity)
        {
            case InvestmentRound round:
                round.RoundType = model.RoundType ?? InvestmentRoundType.Other;
                round.Valuation = model.Valuation;
                round.InvestorNames = CleanList(model.InvestorNames);
                break;

            case GrantRecord grant:
                grant.Institution = model.Institution ?? GrantInstitution.Other;
                grant.ProgramName = Clean(model.ProgramName);
                break;

            case AwardRecord award:
                award.Name = Clean(model.AwardName) ?? "Ödül";
                award.Organization = Clean(model.Organization);
                award.Rank = model.Rank;
                break;

            case ExportRecord export:
                export.TargetCountries = CleanList(model.TargetCountries);
                break;
        }
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static List<string> CleanList(IReadOnlyList<string>? values) =>
        values is null
            ? []
            : [.. values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim())];
}
