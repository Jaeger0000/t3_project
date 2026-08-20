using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Rbac;
using T3.Application.Common.Results;
using T3.Domain.Achievements;

namespace T3.Application.Features.Startups.GetStartupTimeline;

/// <summary>
/// Girişimin gelişim yolculuğu (MVP #2). Çizelge ayrı bir tabloda tutulmaz;
/// program katılımları, başarı kayıtları ve kilometre taşları sorgu anında
/// birleştirilir. Böylece tek bir gerçeklik kaynağı kalır: bir yatırım turu
/// eklendiğinde çizelgeye ayrıca kayıt düşmek gerekmez.
/// </summary>
public sealed class GetStartupTimelineHandler(
    IAppDbContext db,
    IStartupScope scope,
    ICurrentUser currentUser)
{
    public async Task<Result<StartupTimelineResponse>> Handle(Guid id, CancellationToken ct)
    {
        var startup = await scope.Apply(db.Startups.AsNoTracking())
            .Where(s => s.Id == id)
            .Select(s => new { s.Id, s.Name, s.FoundedOn })
            .FirstOrDefaultAsync(ct);

        if (startup is null)
            return Error.NotFound("Girişim bulunamadı.");

        var visibility = StartupVisibility.For(currentUser, startup.Id);
        var entries = new List<TimelineEntryResponse>();

        if (startup.FoundedOn is { } foundedOn)
            entries.Add(new TimelineEntryResponse(
                TimelineEntryKind.Founding, foundedOn,
                $"{startup.Name} kuruldu", null, "Kuruluş",
                null, null, true, null));

        entries.AddRange(await ProgramEntriesAsync(id, ct));
        entries.AddRange(await MilestoneEntriesAsync(id, ct));
        entries.AddRange(await AchievementEntriesAsync(id, visibility, ct));

        return new StartupTimelineResponse(
            startup.Id,
            startup.Name,
            visibility.ShowExactAmounts,
            entries
                .OrderByDescending(e => e.OccurredOn)
                .ThenBy(e => e.Kind)
                .ToList());
    }

    /// <summary>
    /// Katılım hem giriş hem çıkış olarak çizelgeye düşer: "programa katıldı"
    /// ve ayrılış tarihi varsa "programı tamamladı". Yolculuğun süresi ancak
    /// iki uç birlikte görününce anlaşılır.
    /// </summary>
    private async Task<List<TimelineEntryResponse>> ProgramEntriesAsync(
        Guid startupId, CancellationToken ct)
    {
        var rows = await db.ProgramParticipations.AsNoTracking()
            .Where(p => p.StartupId == startupId)
            .Select(p => new
            {
                p.Id,
                ProgramName = p.ProgramTerm.Program.Name,
                TermName = p.ProgramTerm.Name,
                Coordinatorship = p.ProgramTerm.Program.Coordinatorship,
                p.Status,
                p.JoinedOn,
                p.LeftOn,
                p.Notes
            })
            .ToListAsync(ct);

        var entries = new List<TimelineEntryResponse>();

        foreach (var row in rows)
        {
            var label = $"{row.ProgramName} · {row.TermName}";

            entries.Add(new TimelineEntryResponse(
                TimelineEntryKind.ProgramJoined,
                row.JoinedOn,
                $"{label} programına katıldı",
                row.Notes ?? row.Coordinatorship,
                TimelineLabels.Participation(row.Status),
                null, null, true, row.Id));

            if (row.LeftOn is { } leftOn)
                entries.Add(new TimelineEntryResponse(
                    TimelineEntryKind.ProgramCompleted,
                    leftOn,
                    $"{label} programından ayrıldı",
                    null,
                    TimelineLabels.Participation(row.Status),
                    null, null, true, row.Id));
        }

        return entries;
    }

    private async Task<List<TimelineEntryResponse>> MilestoneEntriesAsync(
        Guid startupId, CancellationToken ct)
    {
        var rows = await db.Milestones.AsNoTracking()
            .Where(m => m.StartupId == startupId)
            .ToListAsync(ct);

        return rows
            .Select(m => new TimelineEntryResponse(
                TimelineEntryKind.Milestone,
                m.OccurredOn,
                m.Title,
                m.Description,
                TimelineLabels.Milestone(m.Type),
                null, null, true, m.Id))
            .ToList();
    }

    /// <summary>
    /// Başarı kayıtları. Tutar yetkisi olmayan rol satırı görür, meblağı
    /// görmez — kayıt varlığını saklamak ekosistem resmini bozar, tutarı
    /// açmak KVKK ve ticari gizlilik ihlali olur.
    /// </summary>
    private async Task<List<TimelineEntryResponse>> AchievementEntriesAsync(
        Guid startupId, StartupVisibility visibility, CancellationToken ct)
    {
        var rows = await db.Achievements.AsNoTracking()
            .Where(a => a.StartupId == startupId)
            .ToListAsync(ct);

        return rows.Select(a => Map(a, visibility)).ToList();
    }

    private static TimelineEntryResponse Map(Achievement a, StartupVisibility visibility)
    {
        var amount = visibility.ShowExactAmounts
            ? (a as MoneyAchievement)?.Amount
            : null;
        var currency = amount is null ? null : (a as MoneyAchievement)?.Currency;

        return a switch
        {
            InvestmentRound round => new TimelineEntryResponse(
                TimelineEntryKind.Investment, a.OccurredOn,
                $"{TimelineLabels.InvestmentRound(round.RoundType)} turu kapandı",
                round.InvestorNames.Count > 0
                    ? $"Yatırımcılar: {string.Join(", ", round.InvestorNames)}"
                    : a.Note,
                TimelineLabels.InvestmentRound(round.RoundType),
                amount, currency, a.IsVerified, a.Id),

            GrantRecord grant => new TimelineEntryResponse(
                TimelineEntryKind.Grant, a.OccurredOn,
                $"{TimelineLabels.Institution(grant.Institution)} desteği alındı",
                grant.ProgramName ?? a.Note,
                TimelineLabels.Institution(grant.Institution),
                amount, currency, a.IsVerified, a.Id),

            ExportRecord export => new TimelineEntryResponse(
                TimelineEntryKind.Export, a.OccurredOn,
                $"{TimelineLabels.Period(export.FiscalYear, export.Quarter)} ihracat kaydı",
                export.TargetCountries.Count > 0
                    ? $"Ülkeler: {string.Join(", ", export.TargetCountries)}"
                    : a.Note,
                "İhracat",
                amount, currency, a.IsVerified, a.Id),

            RevenueRecord revenue => new TimelineEntryResponse(
                TimelineEntryKind.Revenue, a.OccurredOn,
                $"{TimelineLabels.Period(revenue.FiscalYear, revenue.Quarter)} ciro kaydı",
                a.Note,
                "Ciro",
                amount, currency, a.IsVerified, a.Id),

            AwardRecord award => new TimelineEntryResponse(
                TimelineEntryKind.Award, a.OccurredOn,
                award.Rank is { } rank
                    ? $"{award.Name} — {rank}. sıra"
                    : award.Name,
                award.Organization ?? a.Note,
                "Ödül",
                null, null, a.IsVerified, a.Id),

            _ => new TimelineEntryResponse(
                TimelineEntryKind.Milestone, a.OccurredOn,
                a.Note ?? "Kayıt", null, null,
                amount, currency, a.IsVerified, a.Id)
        };
    }
}
