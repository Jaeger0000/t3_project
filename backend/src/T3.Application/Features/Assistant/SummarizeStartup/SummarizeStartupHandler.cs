using System.Globalization;
using System.Text.Json;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Application.Features.Assistant.AskAssistant;
using T3.Application.Features.Startups;
using T3.Application.Features.Startups.GetStartupCard;
using T3.Application.Features.Startups.GetStartupTimeline;

namespace T3.Application.Features.Assistant.SummarizeStartup;

/// <summary>
/// Girişim kartındaki "AI özeti" bloğu. Kart ve zaman çizelgesi handler'larının
/// çıktısını özetler; iki handler de kapsam ve maskeleme uyguladığı için özet
/// hiçbir zaman kullanıcının göremeyeceği bir tutarı içeremez.
///
/// Model yoksa özet yine üretilir — sayılardan kurulan şablon cümle. Bu bilinçli:
/// kartın bir bölümünün "anahtar yok" diye boş kalması ürünü eksik gösterir.
/// </summary>
public sealed class SummarizeStartupHandler(
    GetStartupCardHandler card,
    GetStartupTimelineHandler timeline,
    IChatModel model,
    ICurrentUser currentUser)
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    public async Task<Result<StartupSummaryResponse>> Handle(Guid startupId, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Error.Forbidden("Özet için oturum açmalısınız.");

        var cardResult = await card.Handle(startupId, ct);
        if (!cardResult.IsSuccess) return cardResult.Error!;

        var timelineResult = await timeline.Handle(startupId, ct);
        if (!timelineResult.IsSuccess) return timelineResult.Error!;

        var value = cardResult.Value!;
        var entries = timelineResult.Value!;
        var highlights = Highlights(value, entries);

        var summary = model.IsAvailable
            ? await AskModelAsync(value, entries, highlights, ct)
            : Compose(value, highlights);

        return new StartupSummaryResponse(
            value.Id,
            value.Name,
            summary,
            highlights,
            model.IsAvailable ? AssistantMode.Model : AssistantMode.Local,
            model.IsAvailable ? model.Name : "Yerel özet",
            entries.ExactAmountsVisible,
            DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Özetin dayandığı kayıtlar. Model kullanılsa da kullanılmasa da arayüzde
    /// gösterilir: jüri "bu cümle nereden geldi" sorusunu sorduğunda yanıt
    /// ekranda durmalı.
    /// </summary>
    private static IReadOnlyList<string> Highlights(
        StartupCardResponse card, StartupTimelineResponse timeline)
    {
        var lines = new List<string>();

        if (card.FoundedOn is { } founded)
            lines.Add($"{founded.Year} yılında kuruldu, {card.City ?? "şehir bilgisi yok"}.");

        if (card.Programs.Count > 0)
        {
            var names = card.Programs.Select(p => p.ProgramName).Distinct();
            lines.Add($"{card.Programs.Count} program kaydı: {string.Join(", ", names)}.");
        }

        var investments = timeline.Entries
            .Where(e => e.Kind == TimelineEntryKind.Investment)
            .ToList();

        if (investments.Count > 0)
        {
            var latest = investments[0];
            lines.Add(timeline.ExactAmountsVisible && latest.Amount is { } amount
                ? $"Son yatırım turu {latest.OccurredOn.Year}: {Money(amount, latest.Currency)}"
                  + $" (toplam {investments.Count} tur)."
                : $"{investments.Count} yatırım turu var; tutarlar bu rol için maskeli.");
        }

        var grants = timeline.Entries.Count(e => e.Kind == TimelineEntryKind.Grant);
        if (grants > 0)
            lines.Add($"{grants} hibe/destek kaydı.");

        var awards = timeline.Entries.Where(e => e.Kind == TimelineEntryKind.Award).ToList();
        if (awards.Count > 0)
            lines.Add($"{awards.Count} ödül, en yenisi: {awards[0].Title}.");

        var revenue = timeline.Entries.FirstOrDefault(e => e.Kind == TimelineEntryKind.Revenue);
        if (revenue is not null)
            lines.Add(timeline.ExactAmountsVisible && revenue.Amount is { } amount
                ? $"Son ciro kaydı: {Money(amount, revenue.Currency)}."
                : "Ciro kaydı var; tutar bu rol için maskeli.");

        if (card.DocumentCount > 0)
            lines.Add($"{card.DocumentCount} doküman yüklü.");

        return lines;
    }

    private static string Compose(StartupCardResponse card, IReadOnlyList<string> highlights)
    {
        var opening =
            $"{card.Name}, {StartupLabels.Sector(card.Sector)} sektöründe faaliyet gösteren "
            + $"{StartupLabels.Status(card.Status).ToLower(Turkish)} durumdaki bir girişim.";

        if (card.ProductDescription is { Length: > 0 } description)
            opening += $" {description}";

        return highlights.Count == 0
            ? opening + " Ekosistem kaydında henüz program ya da finansal veri yok."
            : opening + " " + string.Join(" ", highlights);
    }

    private async Task<string> AskModelAsync(
        StartupCardResponse card,
        StartupTimelineResponse timeline,
        IReadOnlyList<string> highlights,
        CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new { card, timeline }, JsonOptions);

        var prompt = $"""
            Aşağıdaki girişim kartını ve gelişim yolculuğunu 3-4 cümlelik bir yönetici
            özetine çevir. Yalnızca verilen veriyi kullan, tahmin ekleme. Tutar alanı
            null ise "maskeli" olduğunu belirt, sayı uydurma. Türkçe yaz.

            {payload}
            """;

        var reply = await model.CompleteAsync(
            "Sen T3 Girişim Ekosistemi'nin karar destek asistanısın. Kısa, somut ve "
            + "yalnızca verilen veriye dayalı yönetici özeti yazarsın.",
            [new ChatMessage(ChatRole.User, prompt)],
            [],
            ct);

        // Model boş dönerse kart yine de özet göstermeli.
        return string.IsNullOrWhiteSpace(reply.Text) ? Compose(card, highlights) : reply.Text!;
    }

    private static string Money(decimal amount, string? currency) =>
        $"{amount.ToString("#,##0", Turkish)} {currency ?? StartupMoney.ReportingCurrency}";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
}
