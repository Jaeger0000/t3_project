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
    OpenRouter = 2,

    /// <summary>
    /// DeepSeek'in kendi API'si. Protokol OpenAI uyumlu olduğu için
    /// <see cref="OpenRouterChatModel"/> adaptörü kullanılıyor; fark yalnızca
    /// taban adres, uç yolu ve geçide özgü alanların gönderilmemesinde.
    ///
    /// Açıkça seçilmesi gerekiyor: DeepSeek anahtarları da <c>sk-</c> ile
    /// başlıyor ve OpenRouter'ın <c>sk-or-</c> dışındaki anahtar biçimlerinden
    /// ayırt edilemiyor. Önekten tahmin etmek, yanlış uca giden sessiz bir
    /// 404'ten daha kötü bir hata değil ama teşhisi zor; karar yapılandırmada
    /// açık duruyor (<c>T3_Ai__Provider="DeepSeek"</c>).
    /// </summary>
    DeepSeek = 3
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
    /// Sağlayıcının taban adresi. Boşsa sağlayıcının bilinen adresi kullanılır
    /// (bkz. <see cref="EffectiveBaseUrl"/>); alan vekil sunucu/yerel geçit
    /// senaryosu için var.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Sohbet tamamlama uç yolu. Boşsa sağlayıcıya göre seçilir: OpenRouter
    /// <c>/api/v1/chat/completions</c>, DeepSeek <c>/chat/completions</c>.
    /// İki sağlayıcı aynı gövde şemasını konuşuyor ama **yolları farklı** —
    /// yolu sabit kodlamak, DeepSeek'e geçerken sessiz bir 404 demekti.
    /// </summary>
    public string? CompletionsPath { get; set; }

    /// <summary>
    /// Düşünme (reasoning) eforu — yalnızca destekleyen sağlayıcıya gönderilir.
    /// DeepSeek'te geçerli değerler <c>none</c>, <c>low</c>, <c>high</c>,
    /// <c>max</c>; varsayılanı <c>high</c>.
    ///
    /// Bizim varsayılanımız <c>none</c>: asistanın işi akıl yürütme değil,
    /// doğru aracı seçip aracın döndürdüğü sayıyı Türkçe cümleye çevirmek.
    /// Ölçtüğümüz fark — aynı soru için 23 çıkış jetonuna karşı 41 (17'si
    /// düşünme) — hem gecikme hem fatura demek. Rapor bölümlerinde daha uzun
    /// akıl yürütme istenirse bu değer ortamdan yükseltilir, kod değişmez.
    /// </summary>
    public string? ReasoningEffort { get; set; } = "none";

    public string Model { get; set; } = "qwen/qwen3.8-27b:free";

    /// <summary>
    /// Birincil model yanıt vermezse denenecek modeller — <b>virgülle</b> ayrık
    /// tek satır (ör. <c>"a/b:free,c/d:free"</c>). Dizi yerine tek satır:
    /// sunucudaki <c>.env</c> dosyasında tek değişken düzenlemek, indeksli
    /// (<c>__0</c>, <c>__1</c>) satırları yönetmekten kolay.
    ///
    /// Neden gerekli: ücretsiz model dilimleri tek bir yukarı akış sağlayıcıya
    /// bağlı ve o havuz paylaşımlı. Ölçülen 429 oranı yaklaşık üçte bir —
    /// yani her üç sorudan biri yeniden denemeye, bir kısmı da yedek yola
    /// düşüyordu. OpenRouter <c>models</c> alanını görürse sırayı <b>kendi
    /// tarafında</b> dener: bizim için tek HTTP isteği, kota açısından tek
    /// çağrı, ama kullanıcı için çalışan bir cevap.
    ///
    /// Zincirdeki her modelin <b>araç çağırma</b> desteği olmalı; olmayan bir
    /// model cevabı üretir ama veriye ulaşamaz (bkz. IChatModel.SupportsTools).
    /// </summary>
    public string? FallbackModels { get; set; } =
        "nvidia/nemotron-3-super-120b-a12b:free,inclusionai/ling-3.0-flash-sante:free";

    /// <summary>
    /// Denenecek modeller, birincil başta olmak üzere. Tekrarlar ayıklanır;
    /// zincir tek elemanlıysa <c>models</c> alanı isteğe hiç eklenmez.
    /// </summary>
    public IReadOnlyList<string> ModelChain()
    {
        var chain = new List<string> { Model };

        foreach (var name in (FallbackModels ?? string.Empty)
                 .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (!chain.Contains(name, StringComparer.OrdinalIgnoreCase))
                chain.Add(name);

        return chain;
    }

    public int MaxTokens { get; set; } = 1024;

    /// <summary>Model yanıtı için üst süre. Kullanıcı sohbet panelinde bekliyor.</summary>
    public int TimeoutSeconds { get; set; } = 45;

    /// <summary>
    /// 429/5xx için yeniden deneme sayısı. Ücretsiz modellerde kota sınırı
    /// olağan bir durum, tek denemede pes etmek sohbeti kullanılamaz kılıyor.
    ///
    /// 3'e çıkarıldı: ücretsiz havuzda (ör. <c>qwen/qwen3.8-27b:free</c>)
    /// ölçülen 429 oranı yaklaşık üçte bir — iki denemede her dokuz sorudan
    /// biri yedek yola düşüyordu. Bekleme süresi sağlayıcının
    /// <c>Retry-After</c> başlığından, yoksa üstel (1-2-4 sn) geliyor; üst
    /// sınır <see cref="TimeoutSeconds"/> ile birlikte düşünülmeli.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

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

    /// <summary>Sağlayıcının taban adresi; açık değer her zaman varsayılanı yener.</summary>
    public string EffectiveBaseUrl() =>
        !string.IsNullOrWhiteSpace(BaseUrl)
            ? BaseUrl!
            : ResolveProvider() switch
            {
                AiProvider.Anthropic => "https://api.anthropic.com",
                AiProvider.DeepSeek => "https://api.deepseek.com",
                _ => "https://openrouter.ai"
            };

    /// <summary>Sohbet tamamlama uç yolu; açık değer her zaman varsayılanı yener.</summary>
    public string EffectiveCompletionsPath() =>
        !string.IsNullOrWhiteSpace(CompletionsPath)
            ? CompletionsPath!
            : ResolveProvider() switch
            {
                AiProvider.DeepSeek => "/chat/completions",
                _ => "/api/v1/chat/completions"
            };

    /// <summary>
    /// Yedek model zinciri yalnızca OpenRouter'da anlamlı: sırayı sağlayıcının
    /// kendisi deniyor. DeepSeek'e tanımadığı bir alan göndermemek için burada
    /// karara bağlanıyor — tek modelli sağlayıcıda zincir zaten kavramsal
    /// olarak yok.
    /// </summary>
    public bool SupportsModelChain() => ResolveProvider() == AiProvider.OpenRouter;

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
