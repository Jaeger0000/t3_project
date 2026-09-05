using System.Text.Json.Nodes;

namespace T3.Application.Features.Assistant;

/// <summary>
/// Araç sonuçlarının modele giden kopyasından kişisel veriyi çıkarır.
///
/// REST maskelemesi (<c>StartupVisibility</c>) rol bazlı: yetkisi olan bir
/// Program Yöneticisi ya da Süper Yönetici ekibin iletişim bilgisini,
/// vergi numarasını REST'te normal şekilde görür. Ama AI özelliği açıkken bu
/// veri modele (yurt dışında yerleşik bir sağlayıcıya) gidiyordu — REST'te
/// "görme yetkisi var" ile "bu veriyi üçüncü bir tarafa aktarmayı kabul
/// ettim" aynı şey değil (bkz. G-04, Guvenlik_Denetimi_ve_Iyilestirme_Plani.md).
///
/// Bu yüzden burası REST maskelemesinden <b>ayrı ve ondan daha sıkı</b>: alan
/// adına bakar, rolden bağımsız olarak siler. MCP yolu bunu kullanmaz —
/// MCP'ye bağlanan ajan kendi Bearer jetonuyla, REST'in gördüğü kadarını
/// görmeyi zaten kabul etmiş bir istemcidir; burada süzülen yalnızca
/// panel içi asistanın konuştuğu harici model.
/// </summary>
public static class AiRedaction
{
    private const string Placeholder = "[kişisel veri — modele gönderilmedi]";

    /// <summary>
    /// Herhangi bir aracın JSON çıktısında, iç içe geçmiş her nesnede, bu
    /// adlardan biriyle eşleşen alanın değeri (dolu ise) yerine tutucuyla
    /// değiştirilir. Ad eşleşmesi büyük/küçük harf duyarsız: DTO'lar zamanla
    /// değişse de aynı isimlendirme kuralını (ör. her yerde "Email") koruyor.
    /// </summary>
    private static readonly HashSet<string> RedactedFieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "ContactEmail",
        "ContactPhone",
        "TaxNumber",
        "FullName",
        "Email",
        "Phone",
        "LinkedInUrl",
    };

    public static string Redact(string json)
    {
        var node = JsonNode.Parse(json);
        Walk(node);
        return node!.ToJsonString();
    }

    private static void Walk(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(kv => kv.Key).ToArray())
                {
                    if (RedactedFieldNames.Contains(key) && obj[key] is JsonValue { } value
                        && value.TryGetValue<string>(out var text) && !string.IsNullOrEmpty(text))
                    {
                        obj[key] = Placeholder;
                        continue;
                    }

                    Walk(obj[key]);
                }
                break;

            case JsonArray array:
                foreach (var item in array)
                    Walk(item);
                break;
        }
    }
}
