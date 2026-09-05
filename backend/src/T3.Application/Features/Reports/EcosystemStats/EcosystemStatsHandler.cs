using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Rbac;
using T3.Application.Common.Results;
using T3.Application.Common.Text;
using T3.Application.Features.Achievements;
using T3.Application.Features.Startups;
using T3.Domain.Achievements;
using T3.Domain.Startups;

namespace T3.Application.Features.Reports.EcosystemStats;

/// <summary>
/// Panonun tek veri kaynağı (karar destek). Sorgu <see cref="IStartupScope"/>
/// ile başlar: Program Yöneticisi'nin panosu yalnızca kendi programlarının
/// karnesidir, ayrı bir "rapor kapsamı" kavramı yok.
///
/// Agregatlar bilinçli olarak bellekte hesaplanıyor. Gerekçe: TPH alt tipleri
/// üzerinde GROUP BY yazmak sağlayıcıya özel çeviri riskine giriyor (Application
/// katmanında Npgsql API'si kullanamıyoruz) ve okunması zor. Çekilen satır
/// sayısı kapsamla sınırlı — ekosistem ölçeğinde birkaç bin satır.
/// </summary>
public sealed class EcosystemStatsHandler(
    IAppDbContext db,
    IStartupScope scope,
    ICurrentUser currentUser)
{
    public async Task<Result<EcosystemStatsResponse>> Handle(
        EcosystemStatsRequest request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Error.Forbidden("Bu raporu görüntülemek için oturum açmalısınız.");

        var scoped = Filter(scope.Apply(db.Startups.AsNoTracking()), request);

        // Alt sorgu olarak taşınıyor (IN (SELECT ...)): kimlikleri önce belleğe
        // çekmek büyük kapsamda parametre sınırına takılırdı.
        var scopedIds = scoped.Select(s => s.Id);

        var startups = await scoped
            .Select(s => new { s.Id, s.Name, s.Sector, s.Status, s.City })
            .ToListAsync(ct);

        var participations = await db.ProgramParticipations.AsNoTracking()
            .Where(p => scopedIds.Contains(p.StartupId))
            .Select(p => new
            {
                p.StartupId,
                ProgramId = p.ProgramTerm.ProgramId,
                ProgramName = p.ProgramTerm.Program.Name
            })
            .ToListAsync(ct);

        // Alt tipler ayrı ayrı sorgulanıyor. Tek sorguda `a is InvestmentRound`
        // gibi tip testlerini projeksiyona koymak EF'in TPH çevirisine bağımlılık
        // yaratırdı; OfType<T>() ise ayrıştırıcı kolonu doğrudan süzer.
        var investments = await MoneyRowsAsync<InvestmentRound>(
            scopedIds, a => a.RoundType, _ => null, MoneyKind.Investment, ct);
        var revenues = await MoneyRowsAsync<RevenueRecord>(
            scopedIds, _ => null, a => a.FiscalYear, MoneyKind.Revenue, ct);
        var exports = await MoneyRowsAsync<ExportRecord>(
            scopedIds, _ => null, a => a.FiscalYear, MoneyKind.Export, ct);
        var grants = await MoneyRowsAsync<GrantRecord>(
            scopedIds, _ => null, _ => null, MoneyKind.Grant, ct);

        // Yıl seçilmeden önceki dağılımı taşıyor: açılır liste her zaman
        // kapsamdaki TÜM yılları göstermeli, yoksa bir yıl seçilince diğer
        // yıllar listeden sessizce kaybolurdu.
        var availableYears = investments.Concat(revenues).Concat(exports).Concat(grants)
            .Select(EffectiveYear)
            .Distinct()
            .OrderByDescending(y => y)
            .ToList();

        if (request.Year is { } year)
        {
            investments = investments.Where(r => EffectiveYear(r) == year).ToList();
            revenues = revenues.Where(r => EffectiveYear(r) == year).ToList();
            exports = exports.Where(r => EffectiveYear(r) == year).ToList();
            grants = grants.Where(r => EffectiveYear(r) == year).ToList();
        }

        // `.Year` veritabanı sorgusunda değil bellekte hesaplanıyor: proje
        // Application katmanında sağlayıcıya özel tarih çevirisine güvenmiyor
        // (bkz. CLAUDE.md), diğer yıl kırılımları da (ByYear) aynı sebeple
        // materialize edildikten sonra gruplanıyor.
        var achievementDates = await db.Achievements.AsNoTracking()
            .Where(a => scopedIds.Contains(a.StartupId))
            .Select(a => a.OccurredOn)
            .ToListAsync(ct);

        var achievementCount = request.Year is { } y
            ? achievementDates.Count(d => d.Year == y)
            : achievementDates.Count;

        var visibility = StartupVisibility.Aggregate(currentUser);

        return Build(
            startups.Select(s => new StartupRow(s.Id, s.Name, s.Sector, s.Status, s.City)).ToList(),
            participations
                .Select(p => new ParticipationRow(p.StartupId, p.ProgramId, p.ProgramName))
                .ToList(),
            investments, revenues, exports, grants,
            achievementCount, visibility, currentUser, availableYears);
    }

    private static int EffectiveYear(MoneyRow row) => row.FiscalYear ?? row.OccurredOn.Year;

    /// <summary>
    /// Tek bir başarı alt tipini düzleştirilmiş tutar satırına indirger. Yalnızca
    /// raporlama para birimindeki kayıtlar toplanır: kur dönüşümü olmadan farklı
    /// para birimlerini toplamak yanlış bir toplam üretir.
    /// </summary>
    private async Task<List<MoneyRow>> MoneyRowsAsync<T>(
        IQueryable<Guid> scopedIds,
        Func<T, InvestmentRoundType?> roundType,
        Func<T, int?> fiscalYear,
        MoneyKind kind,
        CancellationToken ct)
        where T : MoneyAchievement
    {
        var rows = await db.Achievements.AsNoTracking()
            .OfType<T>()
            .Where(a => scopedIds.Contains(a.StartupId)
                        && a.Currency == StartupMoney.ReportingCurrency)
            .ToListAsync(ct);

        return rows
            .Select(a => new MoneyRow(
                a.StartupId, a.OccurredOn, a.Amount, roundType(a), fiscalYear(a), kind))
            .ToList();
    }

    private static IQueryable<Startup> Filter(
        IQueryable<Startup> query, EcosystemStatsRequest request)
    {
        if (request.ProgramId is { } programId)
            query = query.Where(s => s.Participations
                .Any(p => p.ProgramTerm.ProgramId == programId));

        if (request.Sector is { } sector)
            query = query.Where(s => s.Sector == sector);

        if (!string.IsNullOrWhiteSpace(request.City))
            query = query.Where(StartupSearch.InCity(request.City));

        return query;
    }

    private sealed record StartupRow(
        Guid Id, string Name, Sector Sector, StartupStatus Status, string? City);

    private sealed record ParticipationRow(Guid StartupId, Guid ProgramId, string ProgramName);

    private enum MoneyKind { Investment, Revenue, Export, Grant }

    /// <summary>Tutar taşıyan kayıtların rapor için düzleştirilmiş hâli.</summary>
    private sealed record MoneyRow(
        Guid StartupId,
        DateOnly OccurredOn,
        decimal Amount,
        InvestmentRoundType? RoundType,
        int? FiscalYear,
        MoneyKind Kind);

    private static EcosystemStatsResponse Build(
        List<StartupRow> startups,
        List<ParticipationRow> participations,
        List<MoneyRow> investments,
        List<MoneyRow> revenues,
        List<MoneyRow> exports,
        List<MoneyRow> grants,
        int achievementCount,
        StartupVisibility visibility,
        ICurrentUser currentUser,
        IReadOnlyList<int> availableYears)
    {
        var latestRevenueYear = revenues.Count == 0 ? (int?)null : revenues.Max(r => r.FiscalYear!.Value);

        var totals = new EcosystemTotals(
            Startups: startups.Count,
            ActiveStartups: startups.Count(s => s.Status == StartupStatus.Active),
            GraduatedStartups: startups.Count(s => s.Status == StartupStatus.Graduated),
            Programs: participations.Select(p => p.ProgramId).Distinct().Count(),
            Participations: participations.Count,
            Achievements: achievementCount,
            InvestedStartups: investments.Select(i => i.StartupId).Distinct().Count(),
            TotalInvestment: Sum(investments, visibility),
            TotalGrant: Sum(grants, visibility),
            TotalExport: Sum(exports, visibility),
            LatestRevenueYear: latestRevenueYear,
            LatestRevenue: latestRevenueYear is { } year
                ? Sum(revenues.Where(r => r.FiscalYear == year), visibility)
                : null);

        return new EcosystemStatsResponse(
            Totals: totals,
            BySector: startups
                .GroupBy(s => s.Sector)
                .Select(g => new CountSlice(g.Key.ToString(), StartupLabels.Sector(g.Key), g.Count()))
                .OrderByDescending(s => s.Count)
                .ToList(),
            ByStatus: startups
                .GroupBy(s => s.Status)
                .Select(g => new CountSlice(g.Key.ToString(), StartupLabels.Status(g.Key), g.Count()))
                .OrderByDescending(s => s.Count)
                .ToList(),
            ByCity: startups
                .Where(s => !string.IsNullOrWhiteSpace(s.City))
                .GroupBy(s => s.City!)
                // Şehir adı etiket olarak olduğu gibi taşınıyor: Türkçe'de
                // küçültme "İ" harfini bozar, gruplama anahtarı da ekrana yazılır.
                .Select(g => new CountSlice(g.Key, g.Key, g.Count()))
                .OrderByDescending(s => s.Count)
                .ThenBy(s => s.Label, StringComparer.CurrentCulture)
                .Take(8)
                .ToList(),
            ByProgram: participations
                .DistinctBy(p => (p.StartupId, p.ProgramId))
                .GroupBy(p => (p.ProgramId, p.ProgramName))
                .Select(g => new CountSlice(
                    g.Key.ProgramId.ToString(), g.Key.ProgramName, g.Count()))
                .OrderByDescending(s => s.Count)
                .ToList(),
            InvestmentByRound: investments
                .GroupBy(i => i.RoundType!.Value)
                .Select(g => new MoneySlice(
                    g.Key.ToString(),
                    AchievementLabels.InvestmentRound(g.Key),
                    g.Count(),
                    Sum(g, visibility)))
                .OrderByDescending(s => s.Count)
                .ToList(),
            InvestmentByYear: ByYear(investments, i => i.OccurredOn.Year, visibility),
            RevenueByYear: ByYear(revenues, r => r.FiscalYear!.Value, visibility),
            TopByInvestment: investments
                .GroupBy(i => i.StartupId)
                .Select(g => new { StartupId = g.Key, Total = g.Sum(i => i.Amount) })
                .OrderByDescending(x => x.Total)
                .Take(5)
                .Join(startups, x => x.StartupId, s => s.Id, (x, s) => new TopStartupSlice(
                    s.Id,
                    s.Name,
                    StartupLabels.Sector(s.Sector),
                    // Dikkat: burada agregat değil **satır** maskelemesi
                    // uygulanıyor. Sıralama ekosistem karnesi sayılır ve Karar
                    // Verici'ye açıktır; tek bir girişimin tutarı ise satır
                    // düzeyi hassas veridir ve aynı role kapalıdır.
                    StartupVisibility.For(currentUser, s.Id).ShowExactAmounts ? x.Total : null))
                .ToList(),
            /*
             * Ciro sıralaması **tek bir yıl** üzerinden kurulur: yılları
             * toplamak "2025'te en çok ciro yapan" sorusunu yanıtlamaz, eski
             * bir girişimi öne çıkarırdı. Yıl süzgeci verilmişse o yıl, yoksa
             * kayıtlardaki en son yıl esas alınır — hangisi olduğunu istemci
             * `RevenueRankingYear` alanından okur.
             *
             * Bu liste olmadan soru araçlarla yanıtlanamıyordu: ciro yalnızca
             * girişim başına `list_achievements` ile geliyordu ve model 30+
             * girişimi tek tek gezmeye çalışıp araç turlarını tüketiyordu.
             */
            TopByRevenue: latestRevenueYear is { } rankingYear
                ? revenues
                    .Where(r => r.FiscalYear == rankingYear)
                    .GroupBy(r => r.StartupId)
                    .Select(g => new { StartupId = g.Key, Total = g.Sum(r => r.Amount) })
                    .OrderByDescending(x => x.Total)
                    .Take(5)
                    .Join(startups, x => x.StartupId, s => s.Id, (x, s) => new TopRevenueSlice(
                        s.Id,
                        s.Name,
                        StartupLabels.Sector(s.Sector),
                        StartupVisibility.For(currentUser, s.Id).ShowExactAmounts ? x.Total : null))
                    .ToList()
                : [],
            RevenueRankingYear: latestRevenueYear,
            AmountsVisible: visibility.ShowExactAmounts,
            Currency: StartupMoney.ReportingCurrency,
            GeneratedAt: DateTimeOffset.UtcNow,
            AvailableYears: availableYears);
    }

    private static IReadOnlyList<MoneySlice> ByYear(
        IEnumerable<MoneyRow> rows, Func<MoneyRow, int> year, StartupVisibility visibility) =>
        rows
            .GroupBy(year)
            .Select(g => new MoneySlice(
                g.Key.ToString(), g.Key.ToString(), g.Count(), Sum(g, visibility)))
            .OrderBy(s => s.Key, StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// Toplam. Yetki yoksa <c>null</c> döner — 0 yazmak "kayıt yok" demek olurdu
    /// ve kullanıcıyı yanıltırdı.
    /// </summary>
    private static decimal? Sum(IEnumerable<MoneyRow> rows, StartupVisibility visibility)
    {
        if (!visibility.ShowExactAmounts)
            return null;

        var list = rows as ICollection<MoneyRow> ?? rows.ToList();
        return list.Count == 0 ? null : list.Sum(r => r.Amount);
    }
}
