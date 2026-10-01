using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using T3.Application.Common.Interfaces;
using T3.Infrastructure.Ai;

namespace T3.Application.Tests;

/// <summary>
/// Sağlayıcı yanıt vermediğinde ne olduğu. Ücretsiz katmanda 429 olağan bir
/// cevap; bu durumun <see cref="ChatModelUnavailableException"/> olarak
/// çıkması, çağıran tarafın yedek yola düşebilmesinin (sohbet → yerel plan,
/// kart → şablon özet) ön koşulu. Genel bir
/// <see cref="HttpRequestException"/> olarak çıktığında aynı durum
/// "beklenmeyen hata" ile aynı yola düşüyor ve kullanıcı HTTP 500 görüyordu.
/// </summary>
public class ModelYanitVermediTests
{
    [Fact]
    public async Task Kota_asimi_yeniden_denemeler_tukenince_ozel_istisnaya_cevrilir()
    {
        var handler = new SahteYanitlar(Kota(), Kota());
        var model = Kur(handler, maxRetries: 1);

        var hata = await Assert.ThrowsAsync<ChatModelUnavailableException>(
            () => model.CompleteAsync("sistem", [new ChatMessage(ChatRole.User, "kaç girişim var")], [], default));

        // Durum kodu mesajda duruyor: günlüğe bakmadan hangi sınırın çarptığı görünsün.
        Assert.Contains("429", hata.Message);
        Assert.Equal(2, handler.Istek);
    }

    [Fact]
    public async Task Kota_asimindan_sonra_basarili_yanit_gelirse_soru_yine_cevaplanir()
    {
        // Yeniden deneme asıl davranış; yedek yol yalnızca son durak.
        var handler = new SahteYanitlar(Kota(), Basarili("34 girişim kayıtlı."));
        var model = Kur(handler, maxRetries: 1);

        var turn = await model.CompleteAsync(
            "sistem", [new ChatMessage(ChatRole.User, "kaç girişim var")], [], default);

        Assert.Equal("34 girişim kayıtlı.", turn.Text);
        Assert.Equal(2, handler.Istek);
    }

    [Fact]
    public async Task Govdede_hata_tasiyan_200_yaniti_da_ayni_istisnayi_uretir()
    {
        // OpenRouter başarı kodu döndürüp gövdede hata taşıyabiliyor; bunu
        // yakalamazsak "model sustu" gibi görünür ve boş cevap ekrana gider.
        var handler = new SahteYanitlar(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = Govde("""{"error":{"message":"upstream provider error","code":500}}""")
        });

        var model = Kur(handler, maxRetries: 0);

        await Assert.ThrowsAsync<ChatModelUnavailableException>(
            () => model.CompleteAsync("sistem", [new ChatMessage(ChatRole.User, "soru")], [], default));
    }

    [Fact]
    public async Task Bos_govde_sessizce_bos_cevaba_donusmez()
    {
        var handler = new SahteYanitlar(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = Govde("null")
        });

        var model = Kur(handler, maxRetries: 0);

        await Assert.ThrowsAsync<ChatModelUnavailableException>(
            () => model.CompleteAsync("sistem", [new ChatMessage(ChatRole.User, "soru")], [], default));
    }

    [Fact]
    public void Yedek_model_zinciri_istege_sirayla_ekleniyor()
    {
        // Sağlayıcı sırayı kendi tarafında deniyor: birincil kota sınırındaysa
        // ikinciye geçiyor ve bu bizim için tek HTTP isteği. Zincir istekte
        // yoksa bu dayanıklılık tümden kayboluyor.
        var options = new AiOptions
        {
            Model = "birincil/model:free",
            FallbackModels = "yedek/bir:free, yedek/iki:free, birincil/model:free"
        };

        var model = Kur(new SahteYanitlar(Basarili("tamam")), maxRetries: 0, options: options);
        var body = model.BuildBody("sistem", [new ChatMessage(ChatRole.User, "soru")], []);

        Assert.Equal("birincil/model:free", body["model"]!.GetValue<string>());

        var zincir = body["models"]!.AsArray().Select(n => n!.GetValue<string>()).ToArray();

        // Birincil başta, tekrar eden ad bir kez.
        Assert.Equal(["birincil/model:free", "yedek/bir:free", "yedek/iki:free"], zincir);
    }

    [Fact]
    public void Yedek_model_yoksa_models_alani_hic_gonderilmiyor()
    {
        // Gereksiz alan göndermek sağlayıcı davranışını açıklanamaz hâle getirir.
        var options = new AiOptions { Model = "tek/model:free", FallbackModels = null };
        var model = Kur(new SahteYanitlar(Basarili("tamam")), maxRetries: 0, options: options);

        var body = model.BuildBody("sistem", [new ChatMessage(ChatRole.User, "soru")], []);

        Assert.Null(body["models"]);
    }

    [Fact]
    public async Task Yaniti_hangi_modelin_urettigi_cevapla_birlikte_geliyor()
    {
        // Rozet "Model: birincil" yazarken cevabı yedek model üretmiş olabilir;
        // yanlış model adı göstermek hiç göstermemekten kötü.
        var handler = new SahteYanitlar(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = Govde(
                "{\"model\":\"yedek/iki:free\",\"choices\":[{\"message\""
                + ":{\"role\":\"assistant\",\"content\":\"cevap\"}}]}")
        });

        var turn = await Kur(handler, maxRetries: 0).CompleteAsync(
            "sistem", [new ChatMessage(ChatRole.User, "soru")], [], default);

        Assert.Equal("yedek/iki:free", turn.Model);
    }

    // --- yardımcılar ---------------------------------------------------------

    private static OpenRouterChatModel Kur(
        SahteYanitlar handler, int maxRetries, AiOptions? options = null)
    {
        options ??= new AiOptions { Model = "test/model:free" };
        options.ApiKey = "sk-or-test";
        options.MaxRetries = maxRetries;

        return new OpenRouterChatModel(
            new HttpClient(handler) { BaseAddress = new Uri("https://openrouter.test") },
            Options.Create(options),
            new AiCapabilityState(),
            NullLogger<OpenRouterChatModel>.Instance);
    }

    /// <summary>
    /// 429 yanıtı. <c>Retry-After: 0</c> bilinçli: gerçek bekleme süresi
    /// üstel (1-2-4 sn) ve test onu beklemek zorunda kalmamalı.
    /// </summary>
    private static HttpResponseMessage Kota()
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = Govde("""{"error":{"message":"rate-limited upstream","code":429}}""")
        };

        response.Headers.Add("Retry-After", "0");
        return response;
    }

    private static HttpResponseMessage Basarili(string text) =>
        new(HttpStatusCode.OK)
        {
            Content = Govde(
                "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\""
                + text + "\"}}]}")
        };

    private static StringContent Govde(string json) =>
        new(json, Encoding.UTF8, "application/json");

    /// <summary>Sırayla verilen yanıtları döndüren HTTP katmanı; son yanıt tekrar eder.</summary>
    private sealed class SahteYanitlar(params HttpResponseMessage[] yanitlar) : HttpMessageHandler
    {
        public int Istek { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var yanit = yanitlar[Math.Min(Istek, yanitlar.Length - 1)];
            Istek++;
            return Task.FromResult(yanit);
        }
    }
}
