namespace T3.Application.Features.Reports.EcosystemStats;

/// <summary>
/// Ekosistem karnesi (karar destek katmanının veri yüzü).
///
/// <see cref="AmountsVisible"/> ayrı taşınır: tutar alanlarının <c>null</c>
/// olması "kayıt yok" ile "yetkiniz yok" arasında ayrım yapmaz, arayüzün bu
/// ayrımı yapabilmesi gerekir (Faz 2'deki "0 ₺" hatasının kaynağı buydu).
/// </summary>
public sealed record EcosystemStatsResponse(
    EcosystemTotals Totals,
    IReadOnlyList<CountSlice> BySector,
    IReadOnlyList<CountSlice> ByStatus,
    IReadOnlyList<CountSlice> ByCity,
    IReadOnlyList<CountSlice> ByProgram,
    IReadOnlyList<MoneySlice> InvestmentByRound,
    IReadOnlyList<MoneySlice> InvestmentByYear,
    IReadOnlyList<MoneySlice> RevenueByYear,
    IReadOnlyList<TopStartupSlice> TopByInvestment,
    bool AmountsVisible,
    string Currency,
    DateTimeOffset GeneratedAt);

public sealed record EcosystemTotals(
    int Startups,
    int ActiveStartups,
    int GraduatedStartups,
    int Programs,
    int Participations,
    int Achievements,
    int InvestedStartups,
    decimal? TotalInvestment,
    decimal? TotalGrant,
    decimal? TotalExport,
    int? LatestRevenueYear,
    decimal? LatestRevenue);

/// <summary>Sayım dilimi. <see cref="Key"/> makine tarafı (enum adı / kimlik),
/// <see cref="Label"/> ekrana yazılan Türkçe karşılığı.</summary>
public sealed record CountSlice(string Key, string Label, int Count);

public sealed record MoneySlice(string Key, string Label, int Count, decimal? Total);

/// <summary>
/// Yatırım sıralaması. Karar Verici bu listeyi görür ama tutarlar maskelenir:
/// sıralamanın kendisi ekosistem karnesi, tekil tutar ise hassas veri.
///
/// Listeye yalnızca yatırım kaydı olan girişimler girer; bu yüzden
/// <see cref="Investment"/> <c>null</c> ise anlamı tektir: "yetkiniz yok".
/// </summary>
public sealed record TopStartupSlice(
    Guid StartupId, string Name, string SectorLabel, decimal? Investment);
