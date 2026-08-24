using System.Text.Json;
using T3.Application.Common.Text;
using T3.Application.Features.Programs.ListPrograms;
using T3.Domain.Startups;

namespace T3.Application.Features.Assistant;

/// <summary>
/// Dil modeli olmadan çalışan yedek planlayıcı.
///
/// Neden var: demo makinesinde API anahtarı bulunmayabilir ve sohbet panelinin
/// "anahtar yok" diye boş dönmesi jüriye özelliği gösteremez. Bu sınıf soruyu
/// anahtar sözcüklerle araç çağrısına çevirir, yanıtı araçların ürettiği
/// özetlerden kurar. Uydurma yok: yanıtta yalnızca araçların döndürdüğü sayılar
/// geçer, veri yoksa "veri yok" der.
/// </summary>
public sealed class OfflineAssistant(ListProgramsHandler programs)
{
    public sealed record PlannedCall(string Tool, JsonElement Arguments);

    // Anahtar sözcükler SearchText.Normalize çıktısıyla karşılaştırılıyor:
    // küçük harf, ama Türkçe harfler korunuyor (ğ, ş, ı, ç, ö, ü). Bu yüzden
    // her sözcüğün hem Türkçe hem şapkasız yazımı listede duruyor — kullanıcı
    // "saglik" da yazabilir.
    private static readonly (string Keyword, Sector Sector)[] Sectors =
    [
        ("savunma", Sector.Defense),
        ("sağlık", Sector.Health), ("saglik", Sector.Health), ("medikal", Sector.Health),
        ("yazılım", Sector.Software), ("yazilim", Sector.Software),
        ("enerji", Sector.Energy),
        ("tarım", Sector.Agriculture), ("tarim", Sector.Agriculture),
        ("eğitim", Sector.Education), ("egitim", Sector.Education),
        ("finans", Sector.Finance), ("fintek", Sector.Finance),
        ("mobilite", Sector.Mobility), ("lojistik", Sector.Mobility),
        ("uzay", Sector.Space),
        ("üretim", Sector.Manufacturing), ("uretim", Sector.Manufacturing),
        ("imalat", Sector.Manufacturing)
    ];

    private static readonly string[] StatsWords =
        ["kaç", "kac", "toplam", "ortalama", "dağılım", "dagilim", "istatistik",
         "ekosistem", "karne", "özet", "ozet", "rapor", "genel"];

    private static readonly string[] ApprovalWords =
        ["onay", "bekleyen", "kuyruk", "değişiklik", "degisiklik"];

    private static readonly string[] InvestmentWords =
        ["yatırım", "yatirim", "seed", "seri a", "melek", "fon", "değerleme", "degerleme"];

    private static readonly string[] ListWords =
        ["hangi", "listele", "girişim", "girisim", "göster", "goster", "bul"];

    public async Task<IReadOnlyList<PlannedCall>> PlanAsync(string question, CancellationToken ct)
    {
        var text = SearchText.Normalize(question);
        var plan = new List<PlannedCall>();

        if (ApprovalWords.Any(text.Contains))
            plan.Add(new PlannedCall("list_pending_approvals", Args(new { })));

        var sector = Sectors.FirstOrDefault(s => text.Contains(s.Keyword)).Sector;
        var hasSector = Sectors.Any(s => text.Contains(s.Keyword));
        var programId = await MatchProgramAsync(text, ct);

        // İstatistik sorusu: süzgeçler varsa karneye taşınır ("savunmada kaç
        // girişim var" → sector=Defense ile ekosistem karnesi).
        if (StatsWords.Any(text.Contains) || plan.Count == 0)
            plan.Add(new PlannedCall("ecosystem_stats", Args(new
            {
                sector = hasSector ? sector.ToString() : null,
                programId = programId?.ToString()
            })));

        // Liste sorusu: "hangi", "listele", sektör ya da program adı geçiyorsa.
        if (hasSector || programId is not null || ListWords.Any(text.Contains))
        {
            plan.Add(new PlannedCall("search_startups", Args(new
            {
                sector = hasSector ? sector.ToString() : null,
                programId = programId?.ToString(),
                sort = InvestmentWords.Any(text.Contains) ? "MostInvestment" : "Name",
                pageSize = 10
            })));
        }

        return plan;
    }

    /// <summary>
    /// Yanıtı araç özetlerinden kurar. Model olmadığı için cümle üretilmiyor;
    /// araçların kendi ürettiği Türkçe özetler sıralanıyor — bu, uydurma riski
    /// olmayan tek yol.
    /// </summary>
    public static string Compose(IReadOnlyList<AssistantToolResult> results)
    {
        if (results.Count == 0)
            return "Soruyu bir veri sorgusuna çeviremedim. Sektör, program ya da girişim adı ekleyerek tekrar deneyin.";

        var lines = results.Select(r => $"• {r.Summary}");
        return string.Join("\n", lines);
    }

    private async Task<Guid?> MatchProgramAsync(string normalizedQuestion, CancellationToken ct)
    {
        var result = await programs.Handle(ct);
        if (!result.IsSuccess)
            return null;

        // En uzun eşleşen ad kazanır: "T3 Kuluçka" ile "T3 Ön Kuluçka" aynı
        // soruda geçtiğinde daha özgül olanı seçmeliyiz.
        return result.Value!
            .Select(p => new { p.Id, Key = SearchText.Normalize(p.Name) })
            .Where(p => normalizedQuestion.Contains(p.Key, StringComparison.Ordinal))
            .OrderByDescending(p => p.Key.Length)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefault();
    }

    private static JsonElement Args(object value) =>
        JsonSerializer.SerializeToElement(value, JsonOptions);

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
}
