using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using T3.Application.Common.Interfaces;

namespace T3.Infrastructure.Ai;

/// <summary>
/// Anthropic Messages API adaptörü.
///
/// Sınırı bilinçli dar tuttuk: sağlayıcıya yalnızca kullanıcının sorusu, sistem
/// yönergesi ve <em>araçların döndürdüğü, zaten maskelenmiş</em> veri gider.
/// Kullanıcının adı, e-postası ya da jetonu gitmez. Anahtar yoksa bu sınıf hiç
/// kaydedilmez (bkz. <see cref="DisabledChatModel"/>).
/// </summary>
public sealed class AnthropicChatModel(
    HttpClient http,
    IOptions<AiOptions> options,
    ILogger<AnthropicChatModel> logger) : IChatModel
{
    private readonly AiOptions _options = options.Value;

    public bool IsAvailable => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public string Name => _options.Model;

    public async Task<ChatTurn> CompleteAsync(
        string systemPrompt,
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ChatTool> tools,
        CancellationToken ct)
    {
        var body = new JsonObject
        {
            ["model"] = _options.Model,
            ["max_tokens"] = _options.MaxTokens,
            ["system"] = systemPrompt,
            ["messages"] = new JsonArray([.. messages.Select(ToMessage)])
        };

        if (tools.Count > 0)
            body["tools"] = new JsonArray([.. tools.Select(t => (JsonNode)new JsonObject
            {
                ["name"] = t.Name,
                ["description"] = t.Description,
                ["input_schema"] = JsonSerializer.SerializeToNode(t.InputSchema)
            })]);

        using var response = await http.PostAsJsonAsync("/v1/messages", body, ct);

        if (!response.IsSuccessStatusCode)
        {
            // Gövde günlüğe yazılmıyor: sağlayıcı hata gövdesinde isteği
            // yansıtabiliyor ve içinde araç sonuçları (girişim verisi) olabilir.
            logger.LogWarning(
                "Dil modeli isteği başarısız: {Status}", (int)response.StatusCode);

            throw new HttpRequestException(
                $"Dil modeli yanıt vermedi (HTTP {(int)response.StatusCode}).");
        }

        var payload = await response.Content.ReadFromJsonAsync<JsonObject>(ct)
            ?? throw new HttpRequestException("Dil modeli boş yanıt döndü.");

        return Parse(payload);
    }

    private static ChatTurn Parse(JsonObject payload)
    {
        var text = new List<string>();
        var calls = new List<ChatToolCall>();

        foreach (var block in payload["content"]?.AsArray() ?? [])
        {
            if (block is not JsonObject item)
                continue;

            switch (item["type"]?.GetValue<string>())
            {
                case "text" when item["text"]?.GetValue<string>() is { Length: > 0 } value:
                    text.Add(value);
                    break;

                case "tool_use":
                    calls.Add(new ChatToolCall(
                        item["id"]?.GetValue<string>() ?? Guid.NewGuid().ToString("N"),
                        item["name"]?.GetValue<string>() ?? string.Empty,
                        // Araç girdisi JsonElement'e çevriliyor: Application
                        // katmanı JsonNode değil JsonElement okuyor.
                        JsonSerializer.SerializeToElement(item["input"] ?? new JsonObject())));
                    break;
            }
        }

        return new ChatTurn(text.Count == 0 ? null : string.Join("\n", text), calls);
    }

    /// <summary>
    /// Ortak mesaj modelini Anthropic blok biçimine çevirir. Araç sonuçları
    /// protokolde <c>user</c> rolünde taşınır — bu, sağlayıcıya özgü bir ayrıntı
    /// ve bilerek burada, adaptörde kalıyor.
    /// </summary>
    private static JsonNode ToMessage(ChatMessage message)
    {
        if (message.Role == ChatRole.Tool)
            return new JsonObject
            {
                ["role"] = "user",
                ["content"] = new JsonArray([.. (message.ToolResults ?? []).Select(r =>
                    (JsonNode)new JsonObject
                    {
                        ["type"] = "tool_result",
                        ["tool_use_id"] = r.CallId,
                        ["content"] = r.Content,
                        ["is_error"] = r.IsError
                    })])
            };

        var blocks = new JsonArray();

        if (!string.IsNullOrWhiteSpace(message.Text))
            blocks.Add(new JsonObject { ["type"] = "text", ["text"] = message.Text });

        foreach (var call in message.ToolCalls ?? [])
            blocks.Add(new JsonObject
            {
                ["type"] = "tool_use",
                ["id"] = call.Id,
                ["name"] = call.Name,
                ["input"] = JsonSerializer.SerializeToNode(call.Arguments)
            });

        return new JsonObject
        {
            ["role"] = message.Role == ChatRole.Assistant ? "assistant" : "user",
            ["content"] = blocks
        };
    }
}
