using System.Globalization;
using System.Text.Json;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Application.Features.Achievements;
using T3.Application.Features.Achievements.ListAchievements;
using T3.Application.Features.Approvals.ListChangeRequests;
using T3.Application.Features.Programs.ListPrograms;
using T3.Application.Features.Reports.EcosystemStats;
using T3.Application.Features.Reports.ExportStartups;
using T3.Application.Features.Startups;
using T3.Application.Features.Startups.GetStartupCard;
using T3.Application.Features.Startups.GetStartupTimeline;
using T3.Application.Features.Startups.SearchStartups;
using T3.Domain.Approvals;
using T3.Domain.Startups;

namespace T3.Application.Features.Assistant;

/// <summary>
/// Araç çağrısının sonucu: modele giden JSON, insana giden özet ve sonucun
/// dokunduğu girişim kimlikleri.
///
/// <see cref="StartupIds"/> varsayılan olarak boş: ekosistem geneli çalışan
/// araçlar (karne, onay kuyruğu) tek bir girişime bağlanamaz; boş liste
/// "girişim yok" demek, "bilinmiyor" değil.
///
/// <see cref="DownloadToken"/>/<see cref="DownloadFileName"/> yalnızca dosya
/// üreten araçlarda dolu; bkz. <see cref="AssistantSourceResponse"/>.
/// </summary>
public sealed record AssistantToolResult(
    string ToolName,
    string Json,
    string Summary,
    IReadOnlyList<Guid> StartupIds,
    string? DownloadToken = null,
    string? DownloadFileName = null);

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
    ListProgramsHandler programs,
    ListChangeRequestsHandler approvals,
    ExportStartupsHandler exportStartups,
    IAssistantExportStore exportStore,
    IOperationRateLimiter rateLimiter,
    ICurrentUser currentUser)
{
    /// <summary>
    /// Saatte 10 dışa aktarma, kullanıcı başına — REST'in CSV "İndir" ucundaki
    /// <c>AuthRateLimit.MassExportPolicy</c> ile aynı kota. Bu araç aynı
    /// kütlesel/hassas veri kümesini ürettiği için sohbet (dakikada 20) ya da
    /// MCP (dakikada 100) kovasının gevşekliğini miras almamalı — HTTP hız
    /// sınırlayıcı bu uç içindeki tek bir aracın maliyetini göremiyor, o
    /// yüzden kontrol burada, işlem düzeyinde tekrarlanıyor.
    /// </summary>
    // GEÇİCİ TEST ÇARPANI (AuthRateLimit.cs'teki ile aynı gerekçe): taban
    // değer 10/saat — CSV export'un MassExportPolicy'siyle aynı kota. Demo/
    // canlıya çıkmadan önce ×1'e (yani 10'a) döndürülmeli.
    private const int ExcelExportHourlyLimit = 10 * 10;

    private const string ExcelExportRateKey = "export-startups-excel";
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
            "Girişimleri arar ve süzer. Sektör, durum, şehir, program, kuruluş yılı ve "
            + "serbest metin ile filtrelenebilir. Yatırım sıralaması için "
            + "sort=MostInvestment kullan. "
            // "2023 yılı kuruluşlu" gibi sorularda foundedYear'ı BURADA
            // gönder — sonuçtaki foundedOn alanını kendin okuyup elemeye
            // çalışma, küçük modelde tarih karşılaştırması hataya açık.
            + "Kuruluş yılına göre süzmek için foundedYear kullan; sonuçları kendin "
            + "tarihe göre eleme. "
            // Model boş tutarı "maskeli" diye yorumluyordu: Süper Yönetici'ye
            // yatırım kaydı olmayan bir girişim için "bu alan sizden gizli"
            // demek, ürünün KVKK anlatısını doğrudan yanlış gösteriyor.
            + "totalInvestment alanı null ise bunun iki sebebi olabilir: girişimin "
            + "yatırım kaydı yoktur ya da tutar bu rol için maskelidir. Hangisi "
            + "olduğunu bu araçtan bilemezsin — 'maskeli' deme, 'kayıt görünmüyor' de.",
            Schema(new
            {
                q = Text("Ad, ürün açıklaması veya şehirde geçen serbest metin."),
                sector = EnumOf<Sector>("Sektör."),
                status = EnumOf<StartupStatus>("Girişimin durumu."),
                city = Text("Şehir adı (tam eşleşme)."),
                programId = Text("Program kimliği (GUID)."),
                foundedYear = Number("Kuruluş yılı (ör. 2023)."),
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
            + "yatırım ve hibe toplamları, yıllara göre eğilim. "
            // "En çok ciro yapan girişimler" sorusu bu alan olmadan
            // yanıtlanamıyordu: model 30+ girişimi tek tek gezmeye çalışıp
            // araç turlarını tüketiyordu.
            + "**Sıralama soruları bu araçla yanıtlanır:** topByInvestment en "
            + "çok yatırım alan, topByRevenue ise revenueRankingYear yılında en "
            + "çok ciro yapan ilk 5 girişimi verir. Girişimleri tek tek gezme.",
            Schema(new
            {
                programId = Text("Program kimliği (GUID) ile daralt."),
                sector = EnumOf<Sector>("Sektör ile daralt."),
                city = Text("Şehir ile daralt."),
                year = Number("Yıl ile daralt; ciro sıralaması da bu yıla göre kurulur.")
            })),

        new("list_programs",
            "Programları listeler: ad, tür, koordinatörlük, girişim sayısı ve "
            + "dönemler. Kullanıcı bir programdan **adıyla** söz ettiğinde önce "
            + "bunu çağır — program kimliğini (GUID) kullanıcıdan isteme, "
            + "buradan çöz. Listede eşleşen ad yoksa öyle bir program yoktur.",
            Schema(new object())),

        new("list_pending_approvals",
            "Bekleyen değişiklik onaylarını listeler. Yalnızca onay yetkisi olan "
            + "kullanıcılar için sonuç döner.",
            Schema(new { startupId = Text("Tek bir girişimin isteklerine daralt.") })),

        new("export_startups_excel",
            "Süzülmüş girişim listesini indirilebilir bir Excel (.xlsx) dosyasına "
            + "dönüştürür. search_startups ile aynı süzgeçleri kabul eder. Kullanıcı "
            + "'excel olarak ver', 'dosya indir', 'tabloya dök', 'liste hâlinde gönder' "
            + "gibi bir istek yaptığında bu aracı çağır — sonuçları kendi metninde "
            + "tablo gibi yazmaya çalışma, gerçek indirilebilir dosya üret. Yanıtın "
            + "altında bir indirme bağlantısı gösterileceğini kullanıcıya söyle.",
            Schema(new
            {
                q = Text("Ad, ürün açıklaması veya şehirde geçen serbest metin."),
                sector = EnumOf<Sector>("Sektör."),
                status = EnumOf<StartupStatus>("Girişimin durumu."),
                city = Text("Şehir adı (tam eşleşme)."),
                programId = Text("Program kimliği (GUID)."),
                foundedYear = Number("Kuruluş yılı (ör. 2023).")
            }))
    ];

    /// <summary>
    /// MCP <c>tools/list</c>/<c>tools/call</c> yüzeyinden hariç tutulan araçlar.
    /// Dosya üreten araçlar (export_startups_excel) indirme jetonunu yalnızca
    /// sohbetin güvenilir ilk taraf istemcisine taşır (bkz. AssistantSourceResponse);
    /// MCP'nin metin-tabanlı JSON-RPC yanıtında ayrı bir yapılandırılmış kanal
    /// yok, jetonu oraya gömmek güven sınırını bulanıklaştırırdı. Araç MCP'de
    /// hiç görünmez, yarım/sessiz çalışmaz (bkz. McpEndpoints).
    /// </summary>
    public static IReadOnlySet<string> McpExcludedTools { get; } =
        new HashSet<string> { "export_startups_excel" };

    public async Task<Result<AssistantToolResult>> InvokeAsync(
        string name, JsonElement args, CancellationToken ct) => name switch
    {
        "search_startups" => await SearchAsync(args, ct),
        "get_startup_card" => await CardAsync(args, ct),
        "get_program_history" => await TimelineAsync(args, ct),
        "list_achievements" => await AchievementsAsync(args, ct),
        "ecosystem_stats" => await StatsAsync(args, ct),
        "list_programs" => await ProgramsAsync(ct),
        "list_pending_approvals" => await ApprovalsAsync(args, ct),
        "export_startups_excel" => await ExportExcelAsync(args, ct),
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
            FoundedYear = Int(args, "foundedYear"),
            Sort = En<StartupSort>(args, "sort") ?? StartupSort.Name,
            PageSize = Int(args, "pageSize") ?? 20
        }, ct);

        if (!result.IsSuccess) return result.Error!;

        var page = result.Value!;
        var names = page.Items.Take(5).Select(i => i.Name);

        return Wrap("search_startups", page, page.TotalCount == 0
            ? "Süzgece uyan girişim yok."
            : $"{page.TotalCount} girişim bulundu: {string.Join(", ", names)}"
              + (page.TotalCount > 5 ? " …" : "."),
            // Toplam değil, sayfadaki kimlikler: arayüz yalnızca modele
            // gerçekten gösterilen kayıtlara bağlantı verebilmeli.
            StartupIdsOf(page.Items));
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
            + $"{value.Achievements.TotalCount} başarı kaydı.",
            [value.Id]);
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
            + (first is { } start ? $", en eskisi {start.ToString("d MMMM yyyy", Turkish)}." : "."),
            [id]);
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
            + (value.ExactAmountsVisible ? "." : " (tutarlar bu rol için maskeli)."),
            [id]);
    }

    private async Task<Result<AssistantToolResult>> StatsAsync(JsonElement args, CancellationToken ct)
    {
        var result = await stats.Handle(new EcosystemStatsRequest(
            Id(args, "programId"), En<Sector>(args, "sector"), Str(args, "city"),
            Int(args, "year")), ct);

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

    /// <summary>
    /// Program adı → kimlik köprüsü. Bu araç yokken model, adıyla sorulan bir
    /// programın kimliğini kullanıcıdan istiyordu; kullanıcı da bilmiyordu.
    /// Boş liste "böyle bir program yok" demek, "bilinmiyor" değil.
    /// </summary>
    private async Task<Result<AssistantToolResult>> ProgramsAsync(CancellationToken ct)
    {
        var result = await programs.Handle(ct);
        if (!result.IsSuccess) return result.Error!;

        var value = result.Value!;
        var names = value.Take(5).Select(p => p.Name);

        return Wrap("list_programs", value, value.Count == 0
            ? "Görebildiğiniz kapsamda program yok."
            : $"{value.Count} program: {string.Join(", ", names)}"
              + (value.Count > 5 ? " …" : "."));
    }

    /// <summary>
    /// Dosyanın kendisi modele gitmez, yalnızca bir jeton — asıl içerik
    /// <see cref="exportStore"/>'da bekliyor ve istemci onu ayrı bir GET
    /// ucundan indiriyor (bkz. AssistantEndpoints, IAssistantExportStore).
    /// </summary>
    private async Task<Result<AssistantToolResult>> ExportExcelAsync(JsonElement args, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            return Error.Forbidden("Dışa aktarma için oturum açmalısınız.");

        if (!rateLimiter.TryConsume(ExcelExportRateKey, userId, ExcelExportHourlyLimit, TimeSpan.FromHours(1)))
            return Error.Forbidden(
                $"Excel dışa aktarma için saatlik sınıra ulaştınız (saatte en çok {ExcelExportHourlyLimit}). "
                + "Bir süre sonra tekrar deneyin.");

        var result = await exportStartups.HandleExcel(new ExportStartupsRequest(
            Q: Str(args, "q"),
            Sector: En<Sector>(args, "sector"),
            Status: En<StartupStatus>(args, "status"),
            ProgramId: Id(args, "programId"),
            City: Str(args, "city"),
            FoundedYear: Int(args, "foundedYear")), ct);

        if (!result.IsSuccess) return result.Error!;

        var export = result.Value!;
        var token = exportStore.Save(userId, new AssistantExportFile(
            export.File.FileName, export.File.ContentType, export.File.Content));

        return Wrap("export_startups_excel",
            new { fileName = export.File.FileName, rowCount = export.StartupIds.Length },
            export.StartupIds.Length == 0
                ? "Süzgece uyan girişim yok, boş bir Excel dosyası hazırlandı."
                : $"'{export.File.FileName}' adında {export.StartupIds.Length} satırlık "
                  + "Excel dosyası hazırlandı; indirme bağlantısı yanıtın altında görünecek.",
            export.StartupIds,
            downloadToken: token,
            downloadFileName: export.File.FileName);
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

    /// <summary>
    /// Arama sonucundaki kimlikler, sonuçtaki sırayı koruyarak. Ayrı ve saf bir
    /// metot olarak duruyor ki veritabanı olmadan doğrulanabilsin.
    /// </summary>
    public static IReadOnlyList<Guid> StartupIdsOf(IEnumerable<StartupListItemResponse> items) =>
        [.. items.Select(i => i.Id).Distinct()];

    /// <param name="startupIds">
    /// Sonucun dokunduğu girişimler. Verilmezse boş kalır — ekosistem geneli
    /// araçlarda doğru olan bu.
    /// </param>
    /// <param name="downloadToken">
    /// Dosya üreten araçlarda dolu; ham dosya modele değil yalnızca jeton
    /// gider (bkz. IAssistantExportStore).
    /// </param>
    private static Result<AssistantToolResult> Wrap(
        string tool, object payload, string summary,
        IReadOnlyList<Guid>? startupIds = null,
        string? downloadToken = null, string? downloadFileName = null) =>
        new AssistantToolResult(
            tool, JsonSerializer.Serialize(payload, Json), summary, startupIds ?? [],
            downloadToken, downloadFileName);

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
