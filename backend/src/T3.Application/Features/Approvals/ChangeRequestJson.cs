using System.Text.Json;
using System.Text.Json.Serialization;

namespace T3.Application.Features.Approvals;

/// <summary>
/// Öneri gövdelerinin (PayloadJson / BeforeJson) tek serileştirme ayarı.
/// Gönderim ile onay arasında günler geçebiliyor, dolayısıyla yazan ve okuyan
/// taraf aynı ayarı kullanmak zorunda; ayarı iki yerde tanımlamak sessizce
/// okunamayan öneri üretirdi.
///
/// İki bilinçli tercih:
/// <list type="bullet">
/// <item>Enum'lar ad olarak yazılır — jsonb kaydı denetim izinde elle
/// okunabilir kalıyor, sayı görünce anlamını aramak gerekmiyor.</item>
/// <item>Null alanlar atlanmaz. "Alan boşaltıldı" ile "alan gönderilmedi"
/// ayrımı diff'in temeli; null'ı atlamak bu ikisini aynı şeye indirirdi.</item>
/// </list>
/// </summary>
public static class ChangeRequestJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Payload alanı zorunlu; silme önerisinde taşınacak değer yok.</summary>
    public const string EmptyPayload = "{}";

    public static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, Options);

    /// <summary>
    /// Öneriyi okur. Bozuk gövdede null döner: onay handler'ı bunu
    /// "uygulanamaz öneri" olarak raporlar, istisna fırlatıp 500 üretmez.
    /// </summary>
    public static T? TryDeserialize<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
