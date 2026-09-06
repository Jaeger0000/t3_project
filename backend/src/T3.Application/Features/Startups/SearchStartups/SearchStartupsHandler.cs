using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Paging;
using T3.Application.Common.Rbac;
using T3.Application.Common.Results;
using T3.Domain.Achievements;

namespace T3.Application.Features.Startups.SearchStartups;

/// <summary>
/// Girişim arama ve filtreleme (MVP #1'in liste ucu). Sorgu her zaman
/// <see cref="IStartupScope"/>'tan geçer: Program Yöneticisi yalnızca kendi
/// programlarından geçmiş girişimleri, girişim kullanıcısı yalnızca kendisini
/// görür. Hiçbir filtre bu daraltmayı atlayamaz.
/// </summary>
public sealed class SearchStartupsHandler(
    IAppDbContext db,
    IStartupScope scope,
    ICurrentUser currentUser)
{
    public async Task<Result<PagedResult<StartupListItemResponse>>> Handle(
        SearchStartupsRequest request, CancellationToken ct)
    {
        var query = scope.Apply(db.Startups.AsNoTracking());

        // Yüklem StartupSearch'te: küçültme + aksan katlaması iki tarafta da
        // uygulanıyor ve aynı kural CSV aktarımı ile pano istatistiğinde de
        // geçerli. Contains kullanılıyor çünkü kullanıcının girdiği % ve _
        // karakterlerini EF tarafında kaçırıyor.
        if (!string.IsNullOrWhiteSpace(request.Q))
            query = query.Where(StartupSearch.Matches(request.Q));

        if (request.Sector is { } sector)
            query = query.Where(s => s.Sector == sector);

        if (request.Status is { } status)
            query = query.Where(s => s.Status == status);

        if (!string.IsNullOrWhiteSpace(request.City))
            query = query.Where(StartupSearch.InCity(request.City));

        if (request.ProgramId is { } programId)
            query = query.Where(s => s.Participations
                .Any(p => p.ProgramTerm.ProgramId == programId));

        // Yıl aralığı bellekte hesaplanıyor (iki DateOnly sınırı), EF'e
        // sağlayıcıya özgü bir ".Year" çevirisi gitmiyor — bkz. CLAUDE.md.
        if (request.FoundedYear is { } foundedYear)
        {
            var yearStart = new DateOnly(foundedYear, 1, 1);
            var yearEnd = new DateOnly(foundedYear, 12, 31);
            query = query.Where(s => s.FoundedOn >= yearStart && s.FoundedOn <= yearEnd);
        }

        var totalCount = await query.CountAsync(ct);

        query = request.Sort switch
        {
            StartupSort.Newest => query.OrderByDescending(s => s.CreatedAt),
            StartupSort.MostInvestment => query.OrderByDescending(s => s.Achievements
                .OfType<InvestmentRound>()
                .Where(a => a.Currency == StartupMoney.ReportingCurrency)
                .Sum(a => a.Amount)),
            _ => query.OrderBy(s => s.Name)
        };

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new StartupListItemResponse(
                s.Id,
                s.Name,
                s.Sector,
                s.City,
                s.Status,
                s.LogoUrl,
                s.FoundedOn,
                s.TechnologyAreas,
                s.Participations
                    .OrderByDescending(p => p.JoinedOn)
                    .Select(p => p.ProgramTerm.Program.Name)
                    .FirstOrDefault(),
                s.Participations.Count,
                0,
                null,
                false,
                StartupMoney.ReportingCurrency))
            .ToListAsync(ct);

        var enriched = await EnrichAsync(items, ct);

        return new PagedResult<StartupListItemResponse>(
            enriched, request.Page, request.PageSize, totalCount);
    }

    /// <summary>
    /// Başarı sayıları ve yatırım toplamlarını sayfa kimlikleri üzerinden ayrı
    /// sorgularla doldurur. Tek sorguda korelasyonlu TPH agregatı yazmak yerine
    /// bunu seçmemizin nedeni okunabilirlik: iki basit GROUP BY, karmaşık bir
    /// alt sorgudan hem daha öngörülebilir hem hata ayıklaması kolay.
    /// </summary>
    private async Task<List<StartupListItemResponse>> EnrichAsync(
        List<StartupListItemResponse> items, CancellationToken ct)
    {
        if (items.Count == 0)
            return items;

        var ids = items.Select(i => i.Id).ToArray();

        var achievementCounts = await db.Achievements.AsNoTracking()
            .Where(a => ids.Contains(a.StartupId))
            .GroupBy(a => a.StartupId)
            .Select(g => new { StartupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.StartupId, x => x.Count, ct);

        var investmentTotals = await db.Achievements.AsNoTracking()
            .OfType<InvestmentRound>()
            .Where(a => ids.Contains(a.StartupId)
                        && a.Currency == StartupMoney.ReportingCurrency)
            .GroupBy(a => a.StartupId)
            .Select(g => new { StartupId = g.Key, Total = g.Sum(a => a.Amount) })
            .ToDictionaryAsync(x => x.StartupId, x => x.Total, ct);

        return items
            .Select(item =>
            {
                var visibility = StartupVisibility.For(currentUser, item.Id);

                // GetValueOrDefault burada kullanılamaz: yatırım turu olmayan
                // girişim için 0 döner ve arayüzde "0 ₺" olarak görünür.
                // Kayıt yokluğu tutar değil, veri yokluğudur.
                decimal? investment = investmentTotals.TryGetValue(item.Id, out var total)
                    ? total
                    : null;

                return item with
                {
                    AchievementCount = achievementCounts.GetValueOrDefault(item.Id),
                    AmountsVisible = visibility.ShowExactAmounts,
                    TotalInvestment = visibility.ShowExactAmounts ? investment : null
                };
            })
            .ToList();
    }
}
