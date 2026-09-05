using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using T3.Application.Common.Interfaces;

namespace T3.Infrastructure.Ai;

/// <summary>
/// OpenRouter (OpenAI uyumlu <c>chat/completions</c>) adaptörü.
///
/// Anthropic adaptörüyle aynı sınırı korur: sağlayıcıya yalnızca kullanıcının
/// sorusu, sistem yönergesi ve <em>araçların döndürdüğü, zaten maskelenmiş</em>
/// veri gider (bkz. <c>AiRedaction</c>).
///
/// Protokol farkları Anthropic'e göre üç yerde toplanıyor ve bilinçli olarak
/// burada, adaptörde kalıyor:
/// 1. Sistem yönergesi ayrı alan değil, mesaj dizisinin ilk satırı.
/// 2. Araç sonuçları tek <c>user</c> mesajında değil, her biri ayrı bir
///    <c>tool</c> mesajı (<c>tool_call_id</c> ile eşleşir).
/// 3. Araç argümanları nesne değil, <b>JSON metni</b> olarak taşınır.
/// </summary>
public sealed class OpenRouterChatModel : IChatModel
{
    private readonly HttpClient _http;
    private readonly AiOptions _options;
    private readonly AiCapabilityState _capabilities;
    private readonly ILogger<OpenRouterChatModel> _logger;

    public OpenRouterChatModel(
        HttpClient http,
        IOptions<AiOptions> options,
        AiCapabilityState capabilities,
        ILogger<OpenRouterChatModel> logger)
    {
        _http = http;
        _options = options.Value;
        _capabilities = capabilities;
        _logger = logger;
    }

    public bool IsAvailable => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public string Name => _options.Model;

    public bool SupportsTools => !_capabilities.ToolsDisabled;

    public async Task<ChatTurn> CompleteAsync(
        string systemPrompt,
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ChatTool> tools,
        CancellationToken ct)
    {
        var useTools = tools.Count > 0 && !_capabilities.ToolsDisabled;

        try
        {
            return await SendAsync(systemPrompt, messages, useTools ? tools : [], ct);
        }
        catch (UnsupportedToolsException)
        {
            // Model function calling'i tanımıyor. Bir kez araçsız tekrarlıyoruz;
            // bundan sonrası için bayrak kalıcı — çağıran taraf melez yola
            // geçecek (bkz. IChatModel.SupportsTools).
            _capabilities.DisableTools();

            _logger.LogWarning(
                "Model {Model} araç çağırmayı desteklemiyor; araçsız yola geçildi.",
                _options.Model);

            return await SendAsync(systemPrompt, messages, [], ct);
        }
    }

    private async Task<ChatTurn> SendAsync(
        string systemPrompt,
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ChatTool> tools,
        CancellationToken ct)
    {
        var payload = await PostWithRetryAsync(BuildBody(systemPrompt, messages, tools), ct);
        return Parse(payload);
    }

    internal JsonObject BuildBody(
        string systemPrompt,
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ChatTool> tools)
    {
        // Sistem yönergesi ilk mesaj olarak giriyor: OpenAI şemasında ayrı bir
        // "system" alanı yok.
        var wire = new JsonArray
        {
            new JsonObject { ["role"] = "system", ["content"] = systemPrompt }
        };

        foreach (var message in messages)
            foreach (var node in ToMessages(message))
                wire.Add(node);

        var body = new JsonObject
        {
            ["model"] = _options.Model,
            ["max_tokens"] = _options.MaxTokens,
            ["messages"] = wire
        };

        if (tools.Count > 0)
        {
            body["tools"] = new JsonArray([.. tools.Select(t => (JsonNode)new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = t.Name,
                    ["description"] = t.Description,
                    // Anthropic "input_schema" derken OpenAI "parameters" diyor;
                    // şemanın kendisi (JSON Schema) aynı.
                    ["parameters"] = JsonSerializer.SerializeToNode(t.InputSchema)
                }
            })]);

            body["tool_choice"] = "auto";
        }

        return body;
    }

    private async Task<JsonObject> PostWithRetryAsync(JsonObject body, CancellationToken ct)
    {
        // Ücretsiz katmanda 429 olağan; üstel bekleme ile birkaç kez deniyoruz.
        for (var attempt = 0; ; attempt++)
        {
            using var response = await _http.PostAsJsonAsync(
                "/api/v1/chat/completions", body, ct);

            if (response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadFromJsonAsync<JsonObject>(ct)
                    ?? throw new HttpRequestException("Dil modeli boş yanıt döndü.");

                // OpenRouter başarı kodu döndürüp gövdede hata taşıyabiliyor;
                // bunu yakalamazsak "model sustu" gibi görünür.
                EnsureNoInlineError(payload);

                return payload;
            }

            var status = (int)response.StatusCode;
            var detail = await ReadErrorDetailAsync(response, ct);

            if (IsToolRejection(response.StatusCode, detail))
                throw new UnsupportedToolsException();

            var retryable = response.StatusCode is HttpStatusCode.TooManyRequests
                or HttpStatusCode.BadGateway
                or HttpStatusCode.ServiceUnavailable
                or HttpStatusCode.GatewayTimeout;

            if (!retryable || attempt >= _options.MaxRetries)
            {
                // Gövde günlüğe yazılmıyor: sağlayıcı hata gövdesinde isteği
                // yansıtabiliyor ve içinde araç sonuçları (girişim verisi) olabilir.
                _logger.LogWarning("Dil modeli isteği başarısız: {Status}", status);

                throw new HttpRequestException(
                    $"Dil modeli yanıt vermedi (HTTP {status}).");
            }

            var wait = response.Headers.RetryAfter?.Delta
                ?? TimeSpan.FromSeconds(Math.Pow(2, attempt));

            _logger.LogInformation(
                "Dil modeli {Status} döndü, {Seconds} sn sonra yeniden denenecek.",
                status, wait.TotalSeconds);

            await Task.Delay(wait, ct);
        }
    }

    /// <summary>
    /// Araç reddi mi, geçici bir hata mı? Sağlayıcılar bunu tek tip bir kodla
    /// bildirmiyor: kimi 400, kimi 404 veriyor ve ayrım yalnızca mesaj metninde
    /// duruyor. Bu yüzden metne bakmak zorundayız.
    /// </summary>
    private static bool IsToolRejection(HttpStatusCode status, string detail) =>
        status is HttpStatusCode.BadRequest or HttpStatusCode.NotFound
        && (detail.Contains("tool", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("function", StringComparison.OrdinalIgnoreCase));

    private static async Task<string> ReadErrorDetailAsync(
        HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(ct);
        }
        catch
        {
            // Hata gövdesi okunamadıysa akış bozulmasın: metin yalnızca
            // "araç reddi mi" ayrımı için kullanılıyor.
            return string.Empty;
        }
    }

    private static void EnsureNoInlineError(JsonObject payload)
    {
        if (payload["error"] is not { } error)
            return;

        var message = error is JsonObject obj
            ? obj["message"]?.GetValue<string>()
            : error.ToString();

        if (message is not null
            && (message.Contains("tool", StringComparison.OrdinalIgnoreCase)
                || message.Contains("function", StringComparison.OrdinalIgnoreCase)))
            throw new UnsupportedToolsException();

        throw new HttpRequestException("Dil modeli hata döndü.");
    }

    public static ChatTurn Parse(JsonObject payload)
    {
        var message = payload["choices"]?.AsArray().FirstOrDefault()?["message"] as JsonObject;

        if (message is null)
            return new ChatTurn(null, []);

        var text = ReadContent(message["content"]);
        var calls = new List<ChatToolCall>();

        foreach (var node in message["tool_calls"]?.AsArray() ?? [])
        {
            if (node is not JsonObject call)
                continue;

            var function = call["function"] as JsonObject;

            calls.Add(new ChatToolCall(
                call["id"]?.GetValue<string>() ?? Guid.NewGuid().ToString("N"),
                function?["name"]?.GetValue<string>() ?? string.Empty,
                ParseArguments(function?["arguments"])));
        }

        return new ChatTurn(string.IsNullOrWhiteSpace(text) ? null : text, calls);
    }

    /// <summary>
    /// <c>content</c> çoğu sağlayıcıda düz metin, bazılarında blok dizisi.
    /// İkisi de destekleniyor — aksi halde model konuşuyor ama ekran boş kalır.
    /// </summary>
    private static string? ReadContent(JsonNode? content) => content switch
    {
        null => null,
        JsonArray blocks => string.Join("\n", blocks
            .OfType<JsonObject>()
            .Select(b => b["text"]?.GetValue<string>())
            .Where(t => !string.IsNullOrWhiteSpace(t))),
        JsonValue value => value.TryGetValue<string>(out var text) ? text : null,
        _ => content.ToString()
    };

    /// <summary>
    /// Argümanlar protokolde <b>JSON metni</b> olarak geliyor. Bozuk ya da boş
    /// metin boş nesneye düşürülüyor: uydurma bir argümanla handler'ı çağırmak,
    /// modelin hiç çağırmamasından daha kötü sonuç üretir.
    /// </summary>
    public static JsonElement ParseArguments(JsonNode? arguments)
    {
        var raw = arguments switch
        {
            null => null,
            JsonValue value when value.TryGetValue<string>(out var text) => text,
            // Şemaya uymayıp nesne gönderen sağlayıcılar da var; onu da kabul et.
            _ => arguments.ToJsonString()
        };

        if (string.IsNullOrWhiteSpace(raw))
            return EmptyObject();

        try
        {
            using var document = JsonDocument.Parse(raw);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return EmptyObject();
        }
    }

    private static JsonElement EmptyObject()
    {
        using var document = JsonDocument.Parse("{}");
        return document.RootElement.Clone();
    }

    /// <summary>
    /// Ortak mesaj modelini OpenAI biçimine çevirir. Tek satır birden çok
    /// mesaja açılabildiği için <see cref="IEnumerable{T}"/> dönüyor: bir araç
    /// turunda kaç sonuç varsa o kadar <c>tool</c> mesajı gerekiyor.
    /// </summary>
    public static IEnumerable<JsonNode> ToMessages(ChatMessage message)
    {
        if (message.Role == ChatRole.Tool)
        {
            foreach (var result in message.ToolResults ?? [])
                yield return new JsonObject
                {
                    ["role"] = "tool",
                    ["tool_call_id"] = result.CallId,
                    // Protokolde "hata" bayrağı yok; modelin anlaması için
                    // içeriğe yazıyoruz.
                    ["content"] = result.IsError ? $"HATA: {result.Content}" : result.Content
                };

            yield break;
        }

        if (message.Role == ChatRole.Assistant)
        {
            var assistant = new JsonObject
            {
                ["role"] = "assistant",
                ["content"] = message.Text
            };

            var calls = message.ToolCalls ?? [];

            if (calls.Count > 0)
                assistant["tool_calls"] = new JsonArray([.. calls.Select(c => (JsonNode)new JsonObject
                {
                    ["id"] = c.Id,
                    ["type"] = "function",
                    ["function"] = new JsonObject
                    {
                        ["name"] = c.Name,
                        // Nesne değil, metin: protokol argümanları serileştirilmiş
                        // bekliyor.
                        ["arguments"] = c.Arguments.GetRawText()
                    }
                })]);

            yield return assistant;
            yield break;
        }

        yield return new JsonObject
        {
            ["role"] = "user",
            ["content"] = message.Text ?? string.Empty
        };
    }

    /// <summary>Modelin araç çağırmayı reddettiğini taşıyan iç sinyal.</summary>
    private sealed class UnsupportedToolsException : Exception;
}
