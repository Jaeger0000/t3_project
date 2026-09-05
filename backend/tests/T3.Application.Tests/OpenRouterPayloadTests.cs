using System.Text.Json;
using System.Text.Json.Nodes;
using T3.Application.Common.Interfaces;
using T3.Infrastructure.Ai;

namespace T3.Application.Tests;

/// <summary>
/// OpenRouter (OpenAI uyumlu) protokolüne çeviri. Bu eşlemenin üç yeri
/// Anthropic'ten ayrılıyor ve üçü de sessizce bozulabilecek cinsten: yanlış
/// yapıldığında sağlayıcı 400 döndürmüyor, modelin eline eksik/yanlış bağlam
/// geçiyor ve cevap "uydurulmuş" görünüyor.
/// </summary>
public class OpenRouterPayloadTests
{
    [Fact]
    public void Arac_argumanlari_metin_olarak_okunur()
    {
        // Protokol argümanları nesne değil, JSON *metni* olarak taşıyor.
        var arguments = OpenRouterChatModel.ParseArguments(
            JsonValue.Create("""{"sector":"Software","pageSize":5}"""));

        Assert.Equal("Software", arguments.GetProperty("sector").GetString());
        Assert.Equal(5, arguments.GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public void Bozuk_arguman_metni_bos_nesneye_duser()
    {
        // Uydurma bir argümanla handler'ı çağırmak, hiç çağırmamaktan kötü:
        // yanlış girişimin verisi doğruymuş gibi cevaba girer.
        var arguments = OpenRouterChatModel.ParseArguments(JsonValue.Create("{bozuk"));

        Assert.Equal(JsonValueKind.Object, arguments.ValueKind);
        Assert.False(arguments.EnumerateObject().Any());
    }

    [Fact]
    public void Bos_arguman_bos_nesneye_duser()
    {
        Assert.Equal(JsonValueKind.Object, OpenRouterChatModel.ParseArguments(null).ValueKind);
        Assert.Equal(
            JsonValueKind.Object,
            OpenRouterChatModel.ParseArguments(JsonValue.Create("")).ValueKind);
    }

    [Fact]
    public void Her_arac_sonucu_ayri_tool_mesaji_olur()
    {
        // Anthropic'te tüm sonuçlar tek "user" mesajındaydı; burada her sonuç
        // kendi tool_call_id'siyle ayrı satır olmak zorunda.
        var message = new ChatMessage(
            ChatRole.Tool,
            ToolResults:
            [
                new ChatToolResult("call_1", "{\"a\":1}"),
                new ChatToolResult("call_2", "{\"b\":2}")
            ]);

        var wire = OpenRouterChatModel.ToMessages(message).ToList();

        Assert.Equal(2, wire.Count);
        Assert.Equal("tool", wire[0]!["role"]!.GetValue<string>());
        Assert.Equal("call_1", wire[0]!["tool_call_id"]!.GetValue<string>());
        Assert.Equal("call_2", wire[1]!["tool_call_id"]!.GetValue<string>());
    }

    [Fact]
    public void Arac_hatasi_icerige_yaziliyor()
    {
        // Protokolde "is_error" alanı yok; model hatayı ancak metinden anlar.
        var message = new ChatMessage(
            ChatRole.Tool,
            ToolResults: [new ChatToolResult("call_1", "girişim bulunamadı", IsError: true)]);

        var wire = OpenRouterChatModel.ToMessages(message).Single();

        Assert.StartsWith("HATA:", wire!["content"]!.GetValue<string>());
    }

    [Fact]
    public void Asistan_turundaki_arac_cagrisi_metin_arguman_tasir()
    {
        using var document = JsonDocument.Parse("""{"startupId":"abc"}""");

        var message = new ChatMessage(
            ChatRole.Assistant,
            "Bakıyorum",
            ToolCalls: [new ChatToolCall("call_9", "get_startup_card", document.RootElement)]);

        var wire = OpenRouterChatModel.ToMessages(message).Single();
        var call = wire!["tool_calls"]!.AsArray()[0]!;

        Assert.Equal("function", call["type"]!.GetValue<string>());
        Assert.Equal("get_startup_card", call["function"]!["name"]!.GetValue<string>());

        // Nesne değil metin: sağlayıcı serileştirilmiş argüman bekliyor.
        var raw = call["function"]!["arguments"]!.GetValue<string>();
        Assert.Contains("startupId", raw);
    }

    [Fact]
    public void Yanittaki_arac_cagrisi_ve_metin_birlikte_okunur()
    {
        var payload = JsonNode.Parse("""
            {"choices":[{"message":{
                "content":"Bakıyorum",
                "tool_calls":[{"id":"call_1","type":"function",
                    "function":{"name":"search_startups","arguments":"{\"sector\":\"Software\"}"}}]
            }}]}
            """)!.AsObject();

        var turn = OpenRouterChatModel.Parse(payload);

        Assert.Equal("Bakıyorum", turn.Text);
        var call = Assert.Single(turn.ToolCalls);
        Assert.Equal("search_startups", call.Name);
        Assert.Equal("Software", call.Arguments.GetProperty("sector").GetString());
    }

    [Fact]
    public void Icerik_blok_dizisi_olarak_da_gelebilir()
    {
        // Bazı sağlayıcılar content'i dizi olarak dönüyor; desteklenmezse model
        // konuşuyor ama ekran boş kalıyor.
        var payload = JsonNode.Parse("""
            {"choices":[{"message":{"content":[{"type":"text","text":"Merhaba"}]}}]}
            """)!.AsObject();

        Assert.Equal("Merhaba", OpenRouterChatModel.Parse(payload).Text);
    }

    [Fact]
    public void Bos_yanit_cokmeden_okunur()
    {
        var turn = OpenRouterChatModel.Parse(JsonNode.Parse("""{"choices":[]}""")!.AsObject());

        Assert.Null(turn.Text);
        Assert.Empty(turn.ToolCalls);
    }

    [Theory]
    [InlineData("sk-ant-abc", AiProvider.Anthropic)]
    [InlineData("sk-or-v1-abc", AiProvider.OpenRouter)]
    [InlineData("tanimsiz-anahtar", AiProvider.OpenRouter)]
    public void Saglayici_anahtar_onekinden_secilir(string key, AiProvider expected)
    {
        var options = new AiOptions { ApiKey = key };

        Assert.Equal(expected, options.ResolveProvider());
    }

    [Fact]
    public void Acik_saglayici_ayari_anahtar_onekini_yener()
    {
        var options = new AiOptions
        {
            ApiKey = "sk-or-v1-abc",
            Provider = AiProvider.Anthropic
        };

        Assert.Equal(AiProvider.Anthropic, options.ResolveProvider());
    }
}
