namespace T3.Application.Features.Startups.GetStartupTimeline;

/// <summary>
/// Zaman çizelgesi girdisinin türü. Arayüz buna göre simge ve renk seçer;
/// başlık/açıklama metinleri sunucuda oluşturulur çünkü çizelge yapısal veri
/// değil anlatıdır ("… programına katıldı", "… yatırım turu kapandı").
/// </summary>
public enum TimelineEntryKind
{
    Founding = 0,
    ProgramJoined = 1,
    ProgramCompleted = 2,
    Milestone = 3,
    Investment = 4,
    Grant = 5,
    Revenue = 6,
    Export = 7,
    Award = 8
}

public sealed record TimelineEntryResponse(
    TimelineEntryKind Kind,
    DateOnly OccurredOn,
    string Title,
    string? Description,
    string? Badge,
    decimal? Amount,
    string? Currency,
    bool IsVerified,
    Guid? SourceId);

public sealed record StartupTimelineResponse(
    Guid StartupId,
    string StartupName,
    bool ExactAmountsVisible,
    IReadOnlyList<TimelineEntryResponse> Entries);
