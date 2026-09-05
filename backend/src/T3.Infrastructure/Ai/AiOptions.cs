namespace T3.Infrastructure.Ai;

/// <summary>
/// Hangi sağlayıcıya bağlanılacağı. <see cref="Auto"/> anahtarın önekine bakar:
/// kurulum başına tek bir ayar değiştirmek yerine anahtarı yapıştırmak yetsin
/// diye — demo makinesinde en sık yapılan hata sağlayıcıyı güncellemeyi
/// unutmaktı.
/// </summary>
public enum AiProvider
{
    Auto = 0,
    Anthropic = 1,
    OpenRouter = 2
}

public class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>
    /// Sağlayıcı API anahtarı. Yalnızca ortam değişkeninden gelir
    /// (<c>T3_Ai__ApiKey</c>); appsettings.json'a yazılmaz. Boşsa sohbet ucu
    /// yerel planlayıcıya düşer, hata vermez.
    /// </summary>
    public string? ApiKey { get; set; }

    public AiProvider Provider { get; set; } = AiProvider.Auto;

    /// <summary>
    /// Sağlayıcının taban adresi. Boşsa sağlayıcının bilinen adresi kullanılır;
    /// alan yalnızca vekil sunucu/yerel geçit senaryosu için var.
    /// </summary>
    public string? BaseUrl { get; set; }

    public string Model { get; set; } = "minimax/minimax-m3:free";

    public int MaxTokens { get; set; } = 1024;

    /// <summary>Model yanıtı için üst süre. Kullanıcı sohbet panelinde bekliyor.</summary>
    public int TimeoutSeconds { get; set; } = 45;

    /// <summary>
    /// 429/5xx için yeniden deneme sayısı. Ücretsiz modellerde kota sınırı
    /// olağan bir durum, tek denemede pes etmek sohbeti kullanılamaz kılıyor.
    /// </summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>
    /// Ajan döngüsünün üst tur sayısı — her tur bir API çağrısı demek.
    /// </summary>
    public int MaxToolTurns { get; set; } = 4;

    /// <summary>
    /// Araç sonucu JSON'unun modele giderken kırpılacağı sınır. Ücretsiz
    /// modellerin bağlam penceresi dar; kırpılmazsa istek sessizce reddediliyor.
    /// </summary>
    public int MaxToolJsonChars { get; set; } = 8000;

    /// <summary>
    /// OpenRouter'ın sıralama sayfasında görünen isteğe bağlı kimlik başlıkları
    /// (<c>HTTP-Referer</c> / <c>X-Title</c>). Zorunlu değil.
    /// </summary>
    public string? SiteUrl { get; set; }

    public string? AppTitle { get; set; }

    /// <summary>
    /// Anahtar öneki sağlayıcıyı ele veriyor: OpenRouter anahtarları
    /// <c>sk-or-</c>, Anthropic anahtarları <c>sk-ant-</c> ile başlıyor.
    /// Açıkça verilmiş <see cref="Provider"/> her zaman öneki yener.
    /// </summary>
    public AiProvider ResolveProvider()
    {
        if (Provider != AiProvider.Auto)
            return Provider;

        if (ApiKey?.StartsWith("sk-ant-", StringComparison.OrdinalIgnoreCase) == true)
            return AiProvider.Anthropic;

        // Tanınmayan önek OpenRouter sayılıyor: OpenRouter birçok sağlayıcının
        // önüne geçen bir geçit ve anahtar biçimleri zamanla değişebiliyor.
        return AiProvider.OpenRouter;
    }
}
