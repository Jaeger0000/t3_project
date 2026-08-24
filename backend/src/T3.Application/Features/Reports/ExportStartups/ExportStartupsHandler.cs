using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Rbac;
using T3.Application.Common.Results;
using T3.Application.Common.Text;
using T3.Application.Features.Startups;
using T3.Domain.Achievements;
using T3.Domain.Startups;

namespace T3.Application.Features.Reports.ExportStartups;

/// <summary>
/// Girişim listesini CSV olarak dışa aktarır. Üç kural:
///
/// 1. Sorgu <see cref="IStartupScope"/>'tan geçer — dışa aktarma, ekranda
///    görülemeyen satırı dosyaya yazmanın arka kapısı değildir.
/// 2. Hassas sütunlar satır bazında <see cref="StartupVisibility"/> ile
///    maskelenir; maskeli hücre boş değil, "yetkiniz yok" yazar.
/// 3. Her indirme denetim izine yazılır: kim, ne zaman, kaç satır aldı.
/// </summary>
public sealed class ExportStartupsHandler(
    IAppDbContext db,
    IStartupScope scope,
    ICurrentUser currentUser,
    IAuditWriter audit)
{
    /// <summary>
    /// Üst sınır. Dışa aktarma tek istekte tüm veritabanını çekebilecek tek uç;
    /// sınırsız bırakmak hem bellek hem sızıntı riski.
    /// </summary>
    private const int MaxRows = 2000;

    public async Task<Result<CsvFile>> Handle(ExportStartupsRequest request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Error.Forbidden("Dışa aktarma için oturum açmalısınız.");

        var query = Filter(scope.Apply(db.Startups.AsNoTracking()), request);

        var startups = await query
            .OrderBy(s => s.Name)
            .Take(MaxRows)
            .Select(s => new Row(
                s.Id, s.Name, s.LegalName, s.TaxNumber, s.Sector, s.Status, s.City,
                s.FoundedOn, s.Website, s.ContactEmail, s.ContactPhone,
                s.TechnologyAreas,
                s.Participations
                    .Select(p => p.ProgramTerm.Program.Name)
                    .Distinct()
                    .ToList(),
                s.TeamMembers.Count))
            .ToListAsync(ct);

        var ids = startups.Select(s => s.Id).ToArray();

        var investments = await SumByStartupAsync<InvestmentRound>(ids, ct);
        var grants = await SumByStartupAsync<GrantRecord>(ids, ct);
        var exports = await SumByStartupAsync<ExportRecord>(ids, ct);

        var revenues = await db.Achievements.AsNoTracking()
            .OfType<RevenueRecord>()
            .Where(a => ids.Contains(a.StartupId)
                        && a.Currency == StartupMoney.ReportingCurrency)
            .Select(a => new { a.StartupId, a.FiscalYear, a.Amount })
            .ToListAsync(ct);

        // Son mali yılın cirosu: girişim başına en büyük yıl seçilip o yılın
        // kayıtları toplanıyor (çeyreklik girişler aynı yıla düşebilir).
        var latestRevenue = revenues
            .GroupBy(r => r.StartupId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var year = g.Max(r => r.FiscalYear);
                    return (Year: year, Total: g.Where(r => r.FiscalYear == year).Sum(r => r.Amount));
                });

        var csv = new CsvBuilder(
            "Girişim", "Ünvan", "Vergi No", "Sektör", "Durum", "Şehir", "Kuruluş",
            "Web", "İletişim e-posta", "İletişim telefon", "Teknoloji alanları",
            "Programlar", "Ekip sayısı", "Toplam yatırım", "Toplam hibe",
            "Toplam ihracat", "Son ciro yılı", "Son ciro", "Para birimi");

        foreach (var row in startups)
        {
            var visibility = StartupVisibility.For(currentUser, row.Id);
            var revenue = latestRevenue.TryGetValue(row.Id, out var found) ? found : default;

            csv.Row(
                row.Name,
                row.LegalName,
                CsvBuilder.Text(row.TaxNumber, visibility.ShowTaxNumber),
                StartupLabels.Sector(row.Sector),
                StartupLabels.Status(row.Status),
                row.City,
                CsvBuilder.Date(row.FoundedOn),
                row.Website,
                CsvBuilder.Text(row.ContactEmail, visibility.ShowContactDetails),
                CsvBuilder.Text(row.ContactPhone, visibility.ShowContactDetails),
                string.Join(", ", row.TechnologyAreas),
                string.Join(", ", row.Programs),
                CsvBuilder.Number(row.TeamCount),
                CsvBuilder.Money(Lookup(investments, row.Id), visibility.ShowExactAmounts),
                CsvBuilder.Money(Lookup(grants, row.Id), visibility.ShowExactAmounts),
                CsvBuilder.Money(Lookup(exports, row.Id), visibility.ShowExactAmounts),
                revenue.Year == 0 ? null : CsvBuilder.Number(revenue.Year),
                CsvBuilder.Money(revenue.Year == 0 ? null : revenue.Total, visibility.ShowExactAmounts),
                StartupMoney.ReportingCurrency);
        }

        await audit.WriteAsync(
            "Report.Export", "Startup", null,
            after: new { Rows = startups.Count, Filter = request },
            ct: ct);

        // Dosya adı tarihli: aynı klasöre indirilen iki rapor birbirini ezmesin.
        var fileName = $"t3-girisimler-{DateTimeOffset.UtcNow:yyyyMMdd-HHmm}.csv";
        return new CsvFile(fileName, "text/csv; charset=utf-8", csv.ToBytes());
    }

    private static decimal? Lookup(Dictionary<Guid, decimal> totals, Guid id) =>
        totals.TryGetValue(id, out var value) ? value : null;

    private async Task<Dictionary<Guid, decimal>> SumByStartupAsync<T>(
        Guid[] ids, CancellationToken ct)
        where T : MoneyAchievement =>
        await db.Achievements.AsNoTracking()
            .OfType<T>()
            .Where(a => ids.Contains(a.StartupId)
                        && a.Currency == StartupMoney.ReportingCurrency)
            .GroupBy(a => a.StartupId)
            .Select(g => new { StartupId = g.Key, Total = g.Sum(a => a.Amount) })
            .ToDictionaryAsync(x => x.StartupId, x => x.Total, ct);

    private static IQueryable<Startup> Filter(
        IQueryable<Startup> query, ExportStartupsRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var term = SearchText.Normalize(request.Q);
            query = query.Where(s =>
                s.Name.ToLower().Contains(term)
                || (s.ProductDescription != null && s.ProductDescription.ToLower().Contains(term))
                || (s.City != null && s.City.ToLower().Contains(term)));
        }

        if (request.Sector is { } sector)
            query = query.Where(s => s.Sector == sector);

        if (request.Status is { } status)
            query = query.Where(s => s.Status == status);

        if (request.ProgramId is { } programId)
            query = query.Where(s => s.Participations
                .Any(p => p.ProgramTerm.ProgramId == programId));

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            var city = SearchText.Normalize(request.City);
            query = query.Where(s => s.City != null && s.City.ToLower() == city);
        }

        return query;
    }

    private sealed record Row(
        Guid Id,
        string Name,
        string? LegalName,
        string? TaxNumber,
        Sector Sector,
        StartupStatus Status,
        string? City,
        DateOnly? FoundedOn,
        string? Website,
        string? ContactEmail,
        string? ContactPhone,
        List<string> TechnologyAreas,
        List<string> Programs,
        int TeamCount);
}
