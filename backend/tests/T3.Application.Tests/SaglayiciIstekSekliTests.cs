using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using T3.Application.Common.Interfaces;
using T3.Infrastructure.Ai;

namespace T3.Application.Tests;

/// <summary>
/// Aynı adaptör iki sağlayıcıya hizmet ediyor (OpenRouter, DeepSeek) ve ikisi
/// aynı gövde şemasını konuşuyor — ama uç yolu ve geçide özgü alanlar farklı.
/// Bu ayrımın sessizce bozulması, sağlayıcı değiştirince 404 ya da 400 demek:
/// ikisi de "model yanıt vermiyor" gibi görünür, sebebi günlükte aranır.
/// </summary>
public class SaglayiciIstekSekliTests
{
    [Fact]
    public async Task DeepSeek_kendi_uc_yoluna_gidiyor()
    {
        // OpenRouter /api/v1/chat/completions, DeepSeek /chat/completions.
        var handler = new IstekYakalayan();
        await Kur(handler, new AiOptions
        {
            Provider = AiProvider.DeepSeek,
            Model = "deepseek-flash"
        }).CompleteAsync("sistem", [new ChatMessage(ChatRole.User, "soru")], [], default);

        Assert.Equal("https://api.deepseek.com/chat/completions", handler.Adres);
    }

    [Fact]
    public async Task OpenRouter_uc_yolu_degismedi()
    {
        var handler = new IstekYakalayan();
        await Kur(handler, new AiOptions
        {
            Provider = AiProvider.OpenRouter,
            Model = "qwen/qwen3.8-27b:free"
        }).CompleteAsync("sistem", [new ChatMessage(ChatRole.User, "soru")], [], default);

        Assert.Equal("https://openrouter.ai/api/v1/chat/completions", handler.Adres);
    }

    [Fact]
    public void DeepSeek_istegine_dusunme_eforu_giriyor_model_zinciri_girmiyor()
    {
        // `models` OpenRouter'a özgü bir geçit alanı; tek modelli sağlayıcıya
        // tanımadığı alanı göndermek 400 riski.
        var body = Kur(new IstekYakalayan(), new AiOptions
        {
            Provider = AiProvider.DeepSeek,
            Model = "deepseek-flash",
            FallbackModels = "baska/model:free",
            ReasoningEffort = "none"
        }).BuildBody("sistem", [new ChatMessage(ChatRole.User, "soru")], []);

        Assert.Null(body["models"]);
        Assert.Equal("none", body["reasoning_effort"]!.GetValue<string>());
        Assert.Equal("deepseek-flash", body["model"]!.GetValue<string>());
    }

    [Fact]
    public void OpenRouter_istegine_dusunme_eforu_girmiyor()
    {
        // Alanı tanımayan geçide göndermek davranışı açıklanamaz hâle getirir.
        var body = Kur(new IstekYakalayan(), new AiOptions
        {
            Provider = AiProvider.OpenRouter,
            Model = "qwen/qwen3.8-27b:free",
            ReasoningEffort = "none"
        }).BuildBody("sistem", [new ChatMessage(ChatRole.User, "soru")], []);

        Assert.Null(body["reasoning_effort"]);
    }

    [Fact]
    public void Acik_taban_adres_ve_yol_varsayilani_yener()
    {
        // Vekil/yerel geçit senaryosu: ikisi de ortamdan gelebilmeli.
        var options = new AiOptions
        {
            Provider = AiProvider.DeepSeek,
            BaseUrl = "http://localhost:9999",
            CompletionsPath = "/v1/chat/completions"
        };

        Assert.Equal("http://localhost:9999", options.EffectiveBaseUrl());
        Assert.Equal("/v1/chat/completions", options.EffectiveCompletionsPath());
    }

    private static OpenRouterChatModel Kur(IstekYakalayan handler, AiOptions options)
    {
        options.ApiKey = "sk-test";
        options.MaxRetries = 0;

        return new OpenRouterChatModel(
            new HttpClient(handler) { BaseAddress = new Uri(options.EffectiveBaseUrl()) },
            Options.Create(options),
            new AiCapabilityState(),
            NullLogger<OpenRouterChatModel>.Instance);
    }

    /// <summary>İsteğin gittiği adresi kaydeden, boş ama geçerli yanıt döndüren katman.</summary>
    private sealed class IstekYakalayan : HttpMessageHandler
    {
        public string? Adres { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Adres = request.RequestUri?.ToString();

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"tamam\"}}]}",
                    Encoding.UTF8, "application/json")
            });
        }
    }
}
