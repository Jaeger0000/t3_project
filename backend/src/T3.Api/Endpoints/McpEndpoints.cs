using System.Text.Json;
using System.Text.Json.Nodes;
using T3.Application.Features.Assistant;

namespace T3.Api.Endpoints;

/// <summary>
/// MCP (Model Context Protocol) sunucu ucu — harici ajanların ekosisteme
/// bağlandığı yer.
///
/// Protokol JSON-RPC 2.0 üzerinden konuşulur ve burada elle uygulanıyor:
/// ihtiyacımız olan üç yöntem (<c>initialize</c>, <c>tools/list</c>,
/// <c>tools/call</c>) için hazır SDK bağımlılığı taşımak, sürüm riskini
/// kazandırdığından fazla yükleyecekti (bkz. teknik plan, risk tablosu).
///
/// Kritik olan protokol değil sınırdır: uç, ardışık düzenin kimlik doğrulaması
/// arkasında durur ve araçlar <see cref="AssistantToolbox"/> üzerinden REST ile
/// aynı handler'ları çağırır. Harici ajan da kendi jetonunun yetkisi kadar
/// görür.
/// </summary>
public static class McpEndpoints
{
    private const string ProtocolVersion = "2024-11-05";

    public static IEndpointRouteBuilder MapMcpEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/mcp", async (
                JsonElement message,
                AssistantToolbox toolbox,
                CancellationToken ct) =>
            {
                var method = Text(message, "method");
                var id = message.TryGetProperty("id", out var rawId) && rawId.ValueKind
                    is not (JsonValueKind.Undefined or JsonValueKind.Null)
                    ? JsonNode.Parse(rawId.GetRawText())
                    : null;

                // Bildirim (id yok): protokol yanıt beklemiyor.
                if (id is null)
                    return Results.NoContent();

                return method switch
                {
                    "initialize" => Ok(id, new JsonObject
                    {
                        ["protocolVersion"] = ProtocolVersion,
                        ["capabilities"] = new JsonObject { ["tools"] = new JsonObject() },
                        ["serverInfo"] = new JsonObject
                        {
                            ["name"] = "t3-ekosistem",
                            ["version"] = "1.0.0"
                        }
                    }),

                    "ping" => Ok(id, new JsonObject()),

                    "tools/list" => Ok(id, new JsonObject
                    {
                        ["tools"] = new JsonArray([.. AssistantToolbox.Catalog.Select(tool =>
                            (JsonNode)new JsonObject
                            {
                                ["name"] = tool.Name,
                                ["description"] = tool.Description,
                                ["inputSchema"] = JsonSerializer.SerializeToNode(tool.InputSchema)
                            })])
                    }),

                    "tools/call" => await CallAsync(id, message, toolbox, ct),

                    _ => Error(id, -32601, $"Bilinmeyen yöntem: {method}")
                };
            })
            .WithTags("MCP")
            .WithSummary("MCP JSON-RPC ucu: initialize, tools/list, tools/call.");

        return app;
    }

    private static async Task<IResult> CallAsync(
        JsonNode id, JsonElement message, AssistantToolbox toolbox, CancellationToken ct)
    {
        if (!message.TryGetProperty("params", out var parameters))
            return Error(id, -32602, "params alanı eksik.");

        var name = Text(parameters, "name");
        if (string.IsNullOrWhiteSpace(name))
            return Error(id, -32602, "Araç adı (params.name) eksik.");

        var arguments = parameters.TryGetProperty("arguments", out var args)
            ? args
            : default;

        var result = await toolbox.InvokeAsync(name, arguments, ct);

        // Araç hatası protokol hatası değil: MCP, aracın başarısızlığını
        // isError bayrağıyla sonuç içinde taşır ki ajan durumu okuyup
        // düzeltebilsin. Yetki reddi de buraya düşer.
        if (!result.IsSuccess)
            return Ok(id, Content(result.Error!.Message, isError: true));

        return Ok(id, Content(result.Value!.Json, isError: false));
    }

    private static JsonObject Content(string text, bool isError) => new()
    {
        ["content"] = new JsonArray(new JsonObject
        {
            ["type"] = "text",
            ["text"] = text
        }),
        ["isError"] = isError
    };

    private static IResult Ok(JsonNode id, JsonNode result) => Results.Json(new JsonObject
    {
        ["jsonrpc"] = "2.0",
        ["id"] = id,
        ["result"] = result
    });

    private static IResult Error(JsonNode id, int code, string messageText) =>
        Results.Json(new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["error"] = new JsonObject { ["code"] = code, ["message"] = messageText }
        });

    private static string? Text(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
