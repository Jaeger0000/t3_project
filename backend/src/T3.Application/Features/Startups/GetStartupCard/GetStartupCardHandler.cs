using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Rbac;
using T3.Application.Common.Results;
using T3.Domain.Achievements;
using T3.Domain.Startups;

namespace T3.Application.Features.Startups.GetStartupCard;

/// <summary>
/// Tek girişimin tam kartını üretir (MVP #1).
///
/// Eşleme bilinçli olarak elle yazıldı: maskeleme koşullu bir karardır ve
/// "hangi alan neden gizlendi" sorusunun cevabı kodda okunabilir kalmalı.
/// Otomatik eşleyici bu mantığı gizler, KVKK denetiminde savunulamaz hale gelir.
/// </summary>
public sealed class GetStartupCardHandler(
    IAppDbContext db,
    IStartupScope scope,
    ICurrentUser currentUser)
{
    public async Task<Result<StartupCardResponse>> Handle(Guid id, CancellationToken ct)
    {
        // Kapsam dışındaki girişim "yok" sayılır; 403 dönmek varlığını sızdırırdı.
        var startup = await scope.Apply(db.Startups.AsNoTracking())
            .Where(s => s.Id == id)
            .Include(s => s.TeamMembers)
            .Include(s => s.Participations)
                .ThenInclude(p => p.ProgramTerm)
                    .ThenInclude(t => t.Program)
            // AsSplitQuery bilinçli olarak yok: Relational'a özel, Application
            // katmanı sağlayıcıyı bilmiyor. Tek girişimde ekip × katılım
            // çarpımı bir düzine satır, bölmeye değecek bir maliyet değil.
            .FirstOrDefaultAsync(ct);

        if (startup is null)
            return Error.NotFound("Girişim bulunamadı.");

        var visibility = StartupVisibility.For(currentUser, startup.Id);

        var achievements = await db.Achievements.AsNoTracking()
            .Where(a => a.StartupId == startup.Id)
            .ToListAsync(ct);

        var documentCount = await db.Documents.AsNoTracking()
            .CountAsync(d => d.StartupId == startup.Id, ct);

        var milestoneCount = await db.Milestones.AsNoTracking()
            .CountAsync(m => m.StartupId == startup.Id, ct);

        return new StartupCardResponse(
            Id: startup.Id,
            Name: startup.Name,
            LegalName: startup.LegalName,
            TaxNumber: visibility.ShowTaxNumber ? startup.TaxNumber : null,
            FoundedOn: startup.FoundedOn,
            Sector: startup.Sector,
            TechnologyAreas: startup.TechnologyAreas,
            ProductDescription: startup.ProductDescription,
            Website: startup.Website,
            LogoUrl: startup.LogoUrl,
            City: startup.City,
            ContactEmail: visibility.ShowContactDetails ? startup.ContactEmail : null,
            ContactPhone: visibility.ShowContactDetails ? startup.ContactPhone : null,
            Status: startup.Status,
            Team: MapTeam(startup, visibility),
            Programs: MapPrograms(startup),
            Achievements: Summarize(achievements, visibility),
            DocumentCount: visibility.ShowDocuments ? documentCount : 0,
            MilestoneCount: milestoneCount,
            CreatedAt: startup.CreatedAt,
            UpdatedAt: startup.UpdatedAt,
            Visibility: new CardVisibilityResponse(
                ContactDetails: visibility.ShowContactDetails,
                TaxNumber: visibility.ShowTaxNumber,
                ExactAmounts: visibility.ShowExactAmounts,
                TeamPersonalData: visibility.ShowTeamPersonalData,
                Documents: visibility.ShowDocuments));
    }

    /// <summary>
    /// Ekip her rolde listelenir; kişisel veri yetkisi yoksa yalnızca ad, ünvan
    /// ve kurucu bilgisi kalır. Ekibin varlığını saklamak gereksiz, iletişim
    /// bilgisini açmak ise KVKK ihlali olurdu.
    /// </summary>
    private static List<CardTeamMemberResponse> MapTeam(
        Startup startup, StartupVisibility visibility) =>
        startup.TeamMembers
            .OrderByDescending(m => m.IsFounder)
            .ThenBy(m => m.FullName)
            .Select(m => new CardTeamMemberResponse(
                Id: m.Id,
                FullName: m.FullName,
                Title: m.Title,
                Email: visibility.ShowTeamPersonalData ? m.Email : null,
                Phone: visibility.ShowTeamPersonalData ? m.Phone : null,
                LinkedInUrl: visibility.ShowTeamPersonalData ? m.LinkedInUrl : null,
                IsFounder: m.IsFounder,
                JoinedOn: m.JoinedOn))
            .ToList();

    private static List<CardParticipationResponse> MapPrograms(Startup startup) =>
        startup.Participations
            .OrderByDescending(p => p.JoinedOn)
            .Select(p => new CardParticipationResponse(
                Id: p.Id,
                ProgramId: p.ProgramTerm.ProgramId,
                ProgramName: p.ProgramTerm.Program.Name,
                ProgramType: p.ProgramTerm.Program.Type,
                Coordinatorship: p.ProgramTerm.Program.Coordinatorship,
                ProgramTermId: p.ProgramTermId,
                TermName: p.ProgramTerm.Name,
                Status: p.Status,
                JoinedOn: p.JoinedOn,
                LeftOn: p.LeftOn,
                Notes: p.Notes))
            .ToList();

    /// <summary>
    /// Finansal özet. Tutar yetkisi yoksa sayılar kalır, meblağlar düşer:
    /// Karar Verici "üç yatırım turu var" bilgisini görür, tutarları görmez —
    /// brifin "finansallar yalnızca agregat" kuralının kart karşılığı.
    /// </summary>
    private static CardAchievementSummaryResponse Summarize(
        List<Achievement> achievements, StartupVisibility visibility)
    {
        const string currency = StartupMoney.ReportingCurrency;

        decimal? SumOf<T>() where T : MoneyAchievement
        {
            var total = achievements.OfType<T>()
                .Where(a => a.Currency == currency)
                .Sum(a => a.Amount);

            return total == 0 ? null : total;
        }

        var latestRevenue = achievements.OfType<RevenueRecord>()
            .Where(r => r.Currency == currency)
            .GroupBy(r => r.FiscalYear)
            .OrderByDescending(g => g.Key)
            .Select(g => new { Year = g.Key, Total = g.Sum(r => r.Amount) })
            .FirstOrDefault();

        var showAmounts = visibility.ShowExactAmounts;

        return new CardAchievementSummaryResponse(
            TotalCount: achievements.Count,
            InvestmentRoundCount: achievements.OfType<InvestmentRound>().Count(),
            AwardCount: achievements.OfType<AwardRecord>().Count(),
            TotalInvestment: showAmounts ? SumOf<InvestmentRound>() : null,
            TotalGrant: showAmounts ? SumOf<GrantRecord>() : null,
            LatestAnnualRevenue: showAmounts ? latestRevenue?.Total : null,
            LatestRevenueYear: latestRevenue?.Year,
            TotalExport: showAmounts ? SumOf<ExportRecord>() : null,
            Currency: currency);
    }
}
