namespace T3.Infrastructure.Ai;

public class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>
    /// Anthropic API anahtarı. Yalnızca ortam değişkeninden gelir
    /// (<c>T3_Ai__ApiKey</c>); appsettings.json'a yazılmaz. Boşsa sohbet ucu
    /// yerel planlayıcıya düşer, hata vermez.
    /// </summary>
    public string? ApiKey { get; set; }

    public string Model { get; set; } = "claude-sonnet-5";

    public int MaxTokens { get; set; } = 1024;

    /// <summary>Model yanıtı için üst süre. Kullanıcı sohbet panelinde bekliyor.</summary>
    public int TimeoutSeconds { get; set; } = 45;
}
