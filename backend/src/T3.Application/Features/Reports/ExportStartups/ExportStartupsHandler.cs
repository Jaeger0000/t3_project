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
/// Girişim listesini dışa aktarır — CSV (<see cref="Handle"/>, REST'in "İndir"
/// düğmesi) ya da Excel (<see cref="HandleExcel"/>, AI asistanının
/// <c>export_startups_excel</c> aracı). Üç kural:
///
/// 1. Sorgu <see cref="IStartupScope"/>'tan geçer — dışa aktarma, ekranda
///    görülemeyen satırı dosyaya yazmanın arka kapısı değildir.
/// 2. Hassas sütunlar satır bazında <see cref="StartupVisibility"/> ile
///    maskelenir; maskeli hücre boş değil, "yetkiniz yok" yazar.
/// 3. Her indirme denetim izine yazılır: kim, ne zaman, kaç satır aldı.
///
/// İki format aynı satır verisini paylaşır (<see cref="FetchAsync"/>) —
/// sorgu ya da maskeleme iki yerde ayrı ayrı güncellenirse biri unutulup
/// formatlar arasında veri sapması doğardı.
/// </summary>
public sealed class ExportStartupsHandler(
    IAppDbContext db,
    IStartupScope scope,
    ICurrentUser currentUser,
    IAuditWriter audit,
    IExcelFileBuilder excelBuilder)
{
    private const int MaxRows = 2000;

    private static readonly string[] Headers =
    [
        "Girişim", "Ünvan", "Vergi No", "Sektör", "Durum", "Şehir", "Kuruluş",
        "Web", "İletişim e-posta", "İletişim telefon", "Teknoloji alanları",
        "Programlar", "Ekip sayısı", "Toplam yatırım", "Toplam hibe",
        "Toplam ihracat", "Son ciro yılı", "Son ciro", "Para birimi"
    ];

    public async Task<Result<CsvFile>> Handle(ExportStartupsRequest request, CancellationToken ct)
    {
        var data = await FetchAsync(request, ct);
        if (!data.IsSuccess) return data.Error!;

        var csv = new CsvBuilder(Headers);
        foreach (var row in data.Value!.Rows)
            csv.Row([.. row]);

        await WriteAuditAsync(data.Value!, request, ct);

        var fileName = $"t3-girisimler-{DateTimeOffset.UtcNow:yyyyMMdd-HHmm}.csv";
        return new CsvFile(fileName, "text/csv; charset=utf-8", csv.ToBytes());
    }

    /// <summary>
    /// AI asistanının <c>export_startups_excel</c> aracı bunu çağırır —
    /// REST'in kullandığı aynı sorgu ve maskeleme, farklı olan yalnızca dosya
    /// biçimi.
    /// </summary>
    public async Task<Result<StartupExcelExport>> HandleExcel(
        ExportStartupsRequest request, CancellationToken ct)
    {
        var data = await FetchAsync(request, ct);
        if (!data.IsSuccess) return data.Error!;

        var content = excelBuilder.Build("Girişimler", Headers, data.Value!.Rows);

        await WriteAuditAsync(data.Value!, request, ct);

        var fileName = $"t3-girisimler-{DateTimeOffset.UtcNow:yyyyMMdd-HHmm}.xlsx";
        var file = new ExcelFile(
            fileName,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            content);

        return new StartupExcelExport(file, data.Value!.StartupIds);
    }

    private async Task<Result<ExportData>> FetchAsync(ExportStartupsRequest request, CancellationToken ct)
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

        var rows = new List<IReadOnlyList<string?>>(startups.Count);

        foreach (var row in startups)
        {
            var visibility = StartupVisibility.For(currentUser, row.Id);
            var revenue = latestRevenue.TryGetValue(row.Id, out var found) ? found : default;

            rows.Add(
            [
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
                StartupMoney.ReportingCurrency
            ]);
        }

        return new ExportData(rows, ids);
    }

    // Satır kimlikleri iz'e yazılıyor: bir sızıntı sonrası "hangi girişimler
    // gitti" sorusu yalnızca satır SAYISIYLA cevaplanamıyordu (bkz. G-07,
    // Guvenlik_Denetimi_ve_Iyilestirme_Plani.md).
    private async Task WriteAuditAsync(
        ExportData data, ExportStartupsRequest request, CancellationToken ct) =>
        await audit.WriteAsync(
            "Report.Export", "Startup", null,
            after: new { Rows = data.Rows.Count, Filter = request, StartupIds = data.StartupIds },
            ct: ct);

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
        // Aktarım listeyle aynı yüklemi kullanıyor: ekranda gördüğü satırların
        // CSV'de farklı çıkması en sinsi rapor hatası olurdu.
        if (!string.IsNullOrWhiteSpace(request.Q))
            query = query.Where(StartupSearch.Matches(request.Q));

        if (request.Sector is { } sector)
            query = query.Where(s => s.Sector == sector);

        if (request.Status is { } status)
            query = query.Where(s => s.Status == status);

        if (request.ProgramId is { } programId)
            query = query.Where(s => s.Participations
                .Any(p => p.ProgramTerm.ProgramId == programId));

        if (!string.IsNullOrWhiteSpace(request.City))
            query = query.Where(StartupSearch.InCity(request.City));

        // Yıl aralığı bellekte hesaplanıyor (iki DateOnly sınırı), EF'e
        // sağlayıcıya özgü bir ".Year" çevirisi gitmiyor — bkz. CLAUDE.md.
        if (request.FoundedYear is { } foundedYear)
        {
            var yearStart = new DateOnly(foundedYear, 1, 1);
            var yearEnd = new DateOnly(foundedYear, 12, 31);
            query = query.Where(s => s.FoundedOn >= yearStart && s.FoundedOn <= yearEnd);
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

    private sealed record ExportData(IReadOnlyList<IReadOnlyList<string?>> Rows, Guid[] StartupIds);
}

/// <summary>Excel dışa aktarmanın sonucu: dosyanın kendisi ve dokunduğu
/// girişim kimlikleri (asistan yanıtındaki kaynak listesi için).</summary>
public sealed record StartupExcelExport(ExcelFile File, Guid[] StartupIds);
