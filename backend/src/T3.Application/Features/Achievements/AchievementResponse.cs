using T3.Application.Common.Rbac;
using T3.Domain.Achievements;

namespace T3.Application.Features.Achievements;

/// <summary>
/// Tek başarı/finans kaydı.
///
/// <paramref name="Amount"/> yetki yoksa <c>null</c> döner ve
/// <paramref name="AmountMasked"/> <c>true</c> olur — arayüz "tutar girilmemiş"
/// ile "tutarı görme yetkiniz yok" ayrımını ancak böyle yapabilir. Faz 2'de bu
/// ayrım olmadığı için maskelenen tutar ekranda <c>0 ₺</c> görünmüştü.
///
/// <paramref name="Title"/> sunucuda üretilir: kayıt anlatısı ("Seri A turu
/// kapandı") tür ve alanların bileşimi, arayüzde yeniden kurulması aynı
/// tabloyu iki yerde tutmak olurdu.
/// </summary>
public sealed record AchievementResponse(
    Guid Id,
    Guid StartupId,
    AchievementKind Kind,
    string KindLabel,
    DateOnly OccurredOn,
    string Title,
    string? Note,

    decimal? Amount,
    string? Currency,
    bool AmountMasked,

    int? FiscalYear,
    int? Quarter,
    string? PeriodLabel,

    InvestmentRoundType? RoundType,
    string? RoundTypeLabel,
    decimal? Valuation,
    IReadOnlyList<string> InvestorNames,

    GrantInstitution? Institution,
    string? InstitutionLabel,
    string? ProgramName,

    string? AwardName,
    string? Organization,
    int? Rank,

    IReadOnlyList<string> TargetCountries,

    bool IsVerified,
    DateTimeOffset? VerifiedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

internal static class AchievementResponses
{
    /// <summary>
    /// Eşleme elle yazıldı: hangi alanın hangi yetkiyle gizlendiği kodda
    /// okunabilir kalmalı (KVKK). Kayıt satırı her rolde görünür, meblağ
    /// görünmez — "üç yatırım turu var" ekosistem bilgisi, tutarı ticari sır.
    /// </summary>
    public static AchievementResponse ToResponse(
        this Achievement a, StartupVisibility visibility)
    {
        var kind = AchievementKinds.Of(a);
        var money = a as MoneyAchievement;
        var periodic = a as PeriodicMoneyAchievement;
        var round = a as InvestmentRound;
        var grant = a as GrantRecord;
        var award = a as AwardRecord;
        var export = a as ExportRecord;

        var showAmounts = visibility.ShowExactAmounts;

        return new AchievementResponse(
            Id: a.Id,
            StartupId: a.StartupId,
            Kind: kind,
            KindLabel: AchievementLabels.Kind(kind),
            OccurredOn: a.OccurredOn,
            Title: Title(a, kind),
            Note: a.Note,

            Amount: showAmounts ? money?.Amount : null,
            Currency: money?.Currency,
            AmountMasked: money is not null && !showAmounts,

            FiscalYear: periodic?.FiscalYear,
            Quarter: periodic?.Quarter,
            PeriodLabel: periodic is null
                ? null
                : AchievementLabels.Period(periodic.FiscalYear, periodic.Quarter),

            RoundType: round?.RoundType,
            RoundTypeLabel: round is null
                ? null
                : AchievementLabels.InvestmentRound(round.RoundType),

            // Değerleme de tutar: aynı yetkiye bağlı, ayrı bir kapı açmıyoruz.
            Valuation: showAmounts ? round?.Valuation : null,
            InvestorNames: round?.InvestorNames ?? [],

            Institution: grant?.Institution,
            InstitutionLabel: grant is null
                ? null
                : AchievementLabels.Institution(grant.Institution),
            ProgramName: grant?.ProgramName,

            AwardName: award?.Name,
            Organization: award?.Organization,
            Rank: award?.Rank,

            TargetCountries: export?.TargetCountries ?? [],

            IsVerified: a.IsVerified,
            VerifiedAt: a.VerifiedAt,
            CreatedAt: a.CreatedAt,
            UpdatedAt: a.UpdatedAt);
    }

    private static string Title(Achievement a, AchievementKind kind) => a switch
    {
        InvestmentRound r => $"{AchievementLabels.InvestmentRound(r.RoundType)} turu",
        GrantRecord g => $"{AchievementLabels.Institution(g.Institution)} desteği",
        AwardRecord w => w.Rank is { } rank ? $"{w.Name} — {rank}. sıra" : w.Name,
        // Küçültme yok: "İhracat" invariant kültürde bozuk küçülüyor
        // (bkz. SearchText.Normalize gerekçesi), etiket olduğu gibi kullanılıyor.
        PeriodicMoneyAchievement p =>
            $"{AchievementLabels.Period(p.FiscalYear, p.Quarter)} {AchievementLabels.Kind(kind)} kaydı",
        _ => AchievementLabels.Kind(kind)
    };
}
