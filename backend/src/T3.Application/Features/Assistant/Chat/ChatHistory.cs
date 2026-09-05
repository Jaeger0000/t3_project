using T3.Application.Common.Interfaces;
using T3.Domain.Assistant;

namespace T3.Application.Features.Assistant.Chat;

/// <summary>
/// Saklanan sohbet satırlarını modele verilecek mesaj dizisine çevirir.
/// Sınıf saf (veritabanı bilmiyor) çünkü buradaki iki kural sessizce bozulursa
/// sohbet ya bağlamını kaybeder ya da sağlayıcıdan 400 alır.
/// </summary>
public static class ChatHistory
{
    /// <summary>Geçmişe alınan en fazla tur sayısı (bir tur = soru + cevap).</summary>
    public const int MaxTurns = 10;

    /// <summary>
    /// Son <see cref="MaxTurns"/> turu kronolojik sırayla döner.
    ///
    /// Yalnızca <b>metin</b> satırları geçmişe giriyor. OpenAI uyumlu protokolde
    /// bir <c>tool_calls</c> mesajının hemen ardından her çağrı için eşleşen bir
    /// <c>tool</c> mesajı gelmek zorunda; eşleşmeyen çift sağlayıcıdan 400
    /// döndürür. Araç bloklarını kalıcı hâle getirip yeniden dizmek bu eşleşmeyi
    /// her turda yeniden kanıtlamak demekti — araç sonuçları zaten o turda
    /// kullanılıp tüketiliyor, sonraki tur gerekirse aracı yeniden çağırır.
    ///
    /// Sıralamada <c>CreatedAt</c> eşitliği rol ile bozuluyor: soru ve cevap
    /// aynı milisaniyeye düşebilir ve cevabın soruyu öncelemesi bağlamı ters
    /// çevirirdi.
    /// </summary>
    public static IReadOnlyList<ChatMessage> Build(IEnumerable<AiConversationMessage> messages) =>
    [
        .. messages
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Role)
            .TakeLast(MaxTurns * 2)
            .Select(m => new ChatMessage(
                m.Role == AiMessageRole.User ? ChatRole.User : ChatRole.Assistant,
                m.Text))
    ];
}
