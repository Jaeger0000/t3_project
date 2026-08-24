using System.Globalization;
using System.Text.Json;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Application.Features.Achievements;
using T3.Application.Features.Achievements.ListAchievements;
using T3.Application.Features.Approvals.ListChangeRequests;
using T3.Application.Features.Reports.EcosystemStats;
using T3.Application.Features.Startups;
using T3.Application.Features.Startups.GetStartupCard;
using T3.Application.Features.Startups.GetStartupTimeline;
using T3.Application.Features.Startups.SearchStartups;
using T3.Domain.Approvals;
using T3.Domain.Startups;

namespace T3.Application.Features.Assistant;

/// <summary>Araç çağrısının sonucu: modele giden JSON ve insana giden özet.</summary>
public sealed record AssistantToolResult(string ToolName, string Json, string Summary);

/// <summary>
/// AI ve MCP'nin ortak araç kutusu.
///
/// Pazarlık dışı kural burada uygulanıyor: her araç REST'in çağırdığı **aynı**
/// Application handler'ını sarar. Ayrı sorgu, ayrı projeksiyon, ayrı maskeleme
/// yok — dolayısıyla ajan, kullanıcının REST'te göremediği hiçbir veriyi
/// göremez. Yeni bir araç eklemek isteyen önce handler'ı yazmak zorunda.
/// </summary>
public sealed class AssistantToolbox(
    SearchStartupsHandler search,
    GetStartupCardHandler card,
    GetStartupTimelineHandler timeline,
    ListAchievementsHandler achievements,
    EcosystemStatsHandler stats,
    ListChangeRequestsHandler approvals)
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    /// <summary>
    /// Araç kataloğu. Şemalar sağlayıcıdan bağımsız JSON Schema; hem MCP
    /// <c>tools/list</c> yanıtı hem model isteği aynı tablodan üretilir ki iki
    /// yüzey birbirinden kaymasın.
    /// </summary>
    public static IReadOnlyList<ChatTool> Catalog { get; } =
    [
        new("search_startups",
            "Girişimleri arar ve süzer. Sektör, durum, şehir, program ve serbest metin "
            + "ile filtrelenebilir. Yatırım sıralaması için sort=MostInvestment kullan.",
            Schema(new
            {
                q = Text("Ad, ürün açıklaması veya şehirde geçen serbest metin."),
                sector = EnumOf<Sector>("Sektör."),
                status = EnumOf<StartupStatus>("Girişimin durumu."),
                city = Text("Şehir adı (tam eşleşme)."),
                programId = Text("Program kimliği (GUID)."),
                sort = EnumOf<StartupSort>("Sıralama."),
                pageSize = Number("Kaç kayıt dönsün (varsayılan 20, en çok 100).")
            })),

        new("get_startup_card",
            "Tek bir girişimin kartını döner: künye, ekip, program geçmişi, başarı özeti. "
            + "Hassas alanlar çağıran kullanıcının yetkisine göre maskelenir.",
            Schema(new { startupId = Text("Girişim kimliği (GUID).") }, "startupId")),

        new("get_program_history",
            "Girişimin kronolojik gelişim yolculuğu: program katılımları, yatırımlar, "
            + "hibeler, ödüller tek zaman çizelgesinde.",
            Schema(new { startupId = Text("Girişim kimliği (GUID).") }, "startupId")),

        new("list_achievements",
            "Girişimin başarı ve finans kayıtlarını döner (ciro, ihracat, yatırım, hibe, ödül).",
            Schema(new
            {
                startupId = Text("Girişim kimliği (GUID)."),
                kind = EnumOf<AchievementKind>("Kayıt türü süzgeci.")
            }, "startupId")),

        new("ecosystem_stats",
            "Ekosistem karnesi: girişim sayıları, sektör/şehir/program dağılımı, "
            + "yatırım ve hibe toplamları, yıllara göre eğilim.",
            Schema(new
            {
                programId = Text("Program kimliği (GUID) ile daralt."),
                sector = EnumOf<Sector>("Sektör ile daralt."),
                city = Text("Şehir ile daralt.")
            })),

        new("list_pending_approvals",
            "Bekleyen değişiklik onaylarını listeler. Yalnızca onay yetkisi olan "
            + "kullanıcılar için sonuç döner.",
            Schema(new { startupId = Text("Tek bir girişimin isteklerine daralt.") }))
    ];

    public async Task<Result<AssistantToolResult>> InvokeAsync(
        string name, JsonElement args, CancellationToken ct) => name switch
    {
        "search_startups" => await SearchAsync(args, ct),
        "get_startup_card" => await CardAsync(args, ct),
        "get_program_history" => await TimelineAsync(args, ct),
        "list_achievements" => await AchievementsAsync(args, ct),
        "ecosystem_stats" => await StatsAsync(args, ct),
        "list_pending_approvals" => await ApprovalsAsync(args, ct),
        _ => Error.NotFound($"Bilinmeyen araç: {name}")
    };

    // --- Araç gövdeleri ---------------------------------------------------

    private async Task<Result<AssistantToolResult>> SearchAsync(JsonElement args, CancellationToken ct)
    {
        var result = await search.Handle(new SearchStartupsRequest
        {
            Q = Str(args, "q"),
            Sector = En<Sector>(args, "sector"),
            Status = En<StartupStatus>(args, "status"),
            City = Str(args, "city"),
            ProgramId = Id(args, "programId"),
            Sort = En<StartupSort>(args, "sort") ?? StartupSort.Name,
            PageSize = Int(args, "pageSize") ?? 20
        }, ct);

        if (!result.IsSuccess) return result.Error!;

        var page = result.Value!;
        var names = page.Items.Take(5).Select(i => i.Name);

        return Wrap("search_startups", page, page.TotalCount == 0
            ? "Süzgece uyan girişim yok."
            : $"{page.TotalCount} girişim bulundu: {string.Join(", ", names)}"
              + (page.TotalCount > 5 ? " …" : "."));
    }

    private async Task<Result<AssistantToolResult>> CardAsync(JsonElement args, CancellationToken ct)
    {
        if (Id(args, "startupId") is not { } id)
            return Error.Validation("startupId zorunlu.");

        var result = await card.Handle(id, ct);
        if (!result.IsSuccess) return result.Error!;

        var value = result.Value!;
        return Wrap("get_startup_card", value,
            $"{value.Name} — {StartupLabels.Sector(value.Sector)}, {value.City ?? "şehir bilgisi yok"}, "
            + $"{StartupLabels.Status(value.Status)}; {value.Programs.Count} program kaydı, "
            + $"{value.Achievements.TotalCount} başarı kaydı.");
    }

    private async Task<Result<AssistantToolResult>> TimelineAsync(JsonElement args, CancellationToken ct)
    {
        if (Id(args, "startupId") is not { } id)
            return Error.Validation("startupId zorunlu.");

        var result = await timeline.Handle(id, ct);
        if (!result.IsSuccess) return result.Error!;

        var value = result.Value!;
        DateOnly? first = value.Entries.Count == 0 ? null : value.Entries[^1].OccurredOn;

        return Wrap("get_program_history", value,
            $"{value.StartupName}: {value.Entries.Count} zaman çizelgesi girdisi"
            + (first is { } start ? $", en eskisi {start.ToString("d MMMM yyyy", Turkish)}." : "."));
    }

    private async Task<Result<AssistantToolResult>> AchievementsAsync(JsonElement args, CancellationToken ct)
    {
        if (Id(args, "startupId") is not { } id)
            return Error.Validation("startupId zorunlu.");

        var result = await achievements.Handle(id, En<AchievementKind>(args, "kind"), ct);
        if (!result.IsSuccess) return result.Error!;

        var value = result.Value!;
        return Wrap("list_achievements", value,
            $"{value.Items.Count} başarı kaydı"
            + (value.ExactAmountsVisible ? "." : " (tutarlar bu rol için maskeli)."));
    }

    private async Task<Result<AssistantToolResult>> StatsAsync(JsonElement args, CancellationToken ct)
    {
        var result = await stats.Handle(new EcosystemStatsRequest(
            Id(args, "programId"), En<Sector>(args, "sector"), Str(args, "city")), ct);

        if (!result.IsSuccess) return result.Error!;

        var value = result.Value!;
        var totals = value.Totals;
        var investment = totals.TotalInvestment is { } sum
            ? $"{sum.ToString("#,##0", Turkish)} {value.Currency} yatırım"
            : value.AmountsVisible ? "yatırım kaydı yok" : "yatırım toplamı maskeli";

        return Wrap("ecosystem_stats", value,
            $"{totals.Startups} girişim ({totals.ActiveStartups} faal), "
            + $"{totals.Participations} program katılımı, {investment}.");
    }

    private async Task<Result<AssistantToolResult>> ApprovalsAsync(JsonElement args, CancellationToken ct)
    {
        var result = await approvals.Handle(new ListChangeRequestsRequest
        {
            Status = ChangeRequestStatus.Pending,
            StartupId = Id(args, "startupId"),
            PageSize = 20
        }, ct);

        if (!result.IsSuccess) return result.Error!;

        var value = result.Value!;
        return Wrap("list_pending_approvals", value,
            $"{value.PendingCount} bekleyen onay isteği.");
    }

    private static Result<AssistantToolResult> Wrap(string tool, object payload, string summary) =>
        new AssistantToolResult(tool, JsonSerializer.Serialize(payload, Json), summary);

    // --- Argüman okuma ----------------------------------------------------
    // Model bazen alanı hiç göndermez, bazen null, bazen boş string gönderir;
    // üçü de "verilmedi" demek. Tek yerde ele alınıyor.

    private static string? Str(JsonElement args, string name) =>
        args.ValueKind == JsonValueKind.Object
        && args.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()
            : null;

    private static Guid? Id(JsonElement args, string name) =>
        Guid.TryParse(Str(args, name), out var id) ? id : null;

    private static int? Int(JsonElement args, string name)
    {
        if (args.ValueKind != JsonValueKind.Object || !args.TryGetProperty(name, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.TryGetInt32(out var number) ? number : null,
            JsonValueKind.String => int.TryParse(value.GetString(), out var parsed) ? parsed : null,
            _ => null
        };
    }

    private static T? En<T>(JsonElement args, string name) where T : struct, Enum =>
        Enum.TryParse<T>(Str(args, name), ignoreCase: true, out var value) ? value : null;

    // --- Şema yardımcıları ------------------------------------------------

    private static object Schema(object properties, params string[] required) =>
        new { type = "object", properties, required };

    private static object Text(string description) =>
        new { type = "string", description };

    private static object Number(string description) =>
        new { type = "integer", description };

    private static object EnumOf<T>(string description) where T : struct, Enum =>
        new { type = "string", description, @enum = Enum.GetNames<T>() };
}
