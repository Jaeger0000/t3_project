using T3.Application.Common.Interfaces;
using T3.Application.Features.Assistant.Chat;
using T3.Domain.Assistant;

namespace T3.Application.Tests;

/// <summary>
/// Saklanan sohbetin modele verilecek geçmişe çevrilmesi. İki kural sessizce
/// bozulabilir: sıra ters dönerse model bağlamı yanlış okur, araç blokları
/// geçmişe sızarsa sağlayıcı eşleşmeyen tool_call/tool çifti yüzünden 400
/// döndürür ve sohbet tümden çalışmaz.
/// </summary>
public class ChatHistoryTests
{
    private static AiConversationMessage Line(AiMessageRole role, string text, int minute) =>
        new()
        {
            Role = role,
            Text = text,
            CreatedAt = new DateTimeOffset(2026, 1, 1, 10, minute, 0, TimeSpan.Zero)
        };

    [Fact]
    public void Kronolojik_sira_korunur()
    {
        // Kayıtlar veritabanından sırasız gelebilir; sıralama burada garanti.
        AiConversationMessage[] messages =
        [
            Line(AiMessageRole.Assistant, "ikinci cevap", 4),
            Line(AiMessageRole.User, "ilk soru", 1),
            Line(AiMessageRole.Assistant, "ilk cevap", 2),
            Line(AiMessageRole.User, "ikinci soru", 3)
        ];

        var history = ChatHistory.Build(messages);

        Assert.Equal(
            ["ilk soru", "ilk cevap", "ikinci soru", "ikinci cevap"],
            history.Select(m => m.Text));
    }

    [Fact]
    public void Ayni_ana_dusen_soru_ve_cevap_soru_once_siralanir()
    {
        // Yerel plan modu bir turu milisaniye altında bitirebilir; eşitlikte
        // rol sırası (User=1, Assistant=2) bağlamı ters çevirmeyi engelliyor.
        AiConversationMessage[] messages =
        [
            Line(AiMessageRole.Assistant, "cevap", 1),
            Line(AiMessageRole.User, "soru", 1)
        ];

        var history = ChatHistory.Build(messages);

        Assert.Equal(ChatRole.User, history[0].Role);
        Assert.Equal("soru", history[0].Text);
    }

    [Fact]
    public void En_fazla_on_tur_gecmise_alinir_ve_en_yenileri_kalir()
    {
        var messages = Enumerable.Range(1, 15)
            .SelectMany(i => new[]
            {
                Line(AiMessageRole.User, $"soru {i}", i * 2),
                Line(AiMessageRole.Assistant, $"cevap {i}", i * 2 + 1)
            })
            .ToArray();

        var history = ChatHistory.Build(messages);

        Assert.Equal(ChatHistory.MaxTurns * 2, history.Count);
        Assert.Equal("soru 6", history[0].Text);
        Assert.Equal("cevap 15", history[^1].Text);
    }

    [Fact]
    public void Gecmiste_arac_blogu_bulunmaz()
    {
        // Geçmiş yalnızca metin taşır: araç çağrısı/sonucu blokları hiç
        // saklanmıyor, dolayısıyla yeniden dizilirken eşleşmeyen çift oluşamaz.
        AiConversationMessage[] messages =
        [
            Line(AiMessageRole.User, "yazılım girişimlerini listele", 1),
            Line(AiMessageRole.Assistant, "3 girişim buldum.", 2)
        ];

        var history = ChatHistory.Build(messages);

        Assert.All(history, m =>
        {
            Assert.Null(m.ToolCalls);
            Assert.Null(m.ToolResults);
            Assert.NotEqual(ChatRole.Tool, m.Role);
        });
    }

    [Fact]
    public void Bos_sohbet_bos_gecmis_uretir()
    {
        Assert.Empty(ChatHistory.Build([]));
    }
}
