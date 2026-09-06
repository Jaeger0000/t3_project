using System.Text.Json;

namespace T3.Application.Common.Interfaces;

/// <summary>Modele tanıtılan araç. Şema, sağlayıcıdan bağımsız JSON Schema nesnesi.</summary>
public sealed record ChatTool(string Name, string Description, object InputSchema);

public enum ChatRole { User, Assistant, Tool }

/// <summary>
/// Konuşma satırı. Bir satır ya metin, ya araç çağrısı, ya da araç sonucu taşır;
/// üçü aynı kayıtta durur çünkü sağlayıcıların tamamı bunları tek mesaj
/// dizisinde sıralı bekliyor.
/// </summary>
public sealed record ChatMessage(
    ChatRole Role,
    string? Text = null,
    IReadOnlyList<ChatToolCall>? ToolCalls = null,
    IReadOnlyList<ChatToolResult>? ToolResults = null);

public sealed record ChatToolCall(string Id, string Name, JsonElement Arguments);

public sealed record ChatToolResult(string CallId, string Content, bool IsError = false);

/// <summary>Modelin bir turu: ya konuşuyor ya araç çağırıyor, ikisi birden de olabilir.</summary>
public sealed record ChatTurn(string? Text, IReadOnlyList<ChatToolCall> ToolCalls);

/// <summary>
/// Dil modeli adaptörü. Application yalnızca bu arayüzü bilir; hangi sağlayıcı
/// olduğu Infrastructure'da kalır.
///
/// <see cref="IsAvailable"/> ayrı bir bayrak olarak duruyor: anahtar tanımlı
/// değilken sistemin çökmesi yerine, sohbet ucunun anahtar gerektirmeyen
/// yerel plana düşmesini istiyoruz (demo makinesinde anahtar olmayabilir).
/// </summary>
public interface IChatModel
{
    bool IsAvailable { get; }

    /// <summary>Arayüzde "hangi model yanıtladı" bilgisini göstermek için.</summary>
    string Name { get; }

    /// <summary>
    /// Model araç çağırabiliyor mu? Ücretsiz/küçük modellerin bir kısmı
    /// function calling'i hiç desteklemiyor ve araç gönderilen isteği tümden
    /// reddediyor. Adaptör bunu ilk redde öğrenip <c>false</c>'a çeviriyor;
    /// çağıran taraf da araçları kendisi çalıştırıp sonucu modele metin olarak
    /// veren melez yola geçiyor. Varsayılan <c>true</c> — mevcut sağlayıcıların
    /// davranışı değişmiyor.
    /// </summary>
    bool SupportsTools => true;

    /// <param name="maxTokens">
    /// Bu çağrıya özel üst sınır; verilmezse <c>AiOptions.MaxTokens</c>
    /// kullanılır. Kısa sohbet/özet yanıtları için varsayılan yeterliyken,
    /// çok paragraflı rapor bölümleri (bkz. GenerateStartupReportHandler)
    /// varsayılanla ortasından kesiliyordu — global sınırı yükseltmek yerine
    /// yalnızca bu tür çağrılar kendi üst sınırını istiyor.
    /// </param>
    Task<ChatTurn> CompleteAsync(
        string systemPrompt,
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ChatTool> tools,
        CancellationToken ct,
        int? maxTokens = null);
}
