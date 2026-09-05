using T3.Domain.Common;

namespace T3.Domain.Assistant;

public enum AiMessageRole
{
    User = 1,
    Assistant = 2
}

/// <summary>
/// Sohbetin tek satırı. Yalnızca <b>metin</b> turları saklanıyor; araç
/// çağrısı/araç sonucu blokları kalıcı değil — bir sonraki turda modele geri
/// beslenmeyecekleri için (bkz. <c>ChatHistory</c>) saklamak yalnızca araç
/// çıktısındaki kişisel veriyi ikinci bir tabloya kopyalamak olurdu. Hangi
/// araçların çalıştığı ve hangi girişimlere dokunulduğu, ham çıktı yerine
/// <see cref="ToolNamesJson"/> / <see cref="StartupIdsJson"/> özetinde durur.
/// </summary>
public class AiConversationMessage : Entity
{
    public Guid ConversationId { get; set; }
    public AiConversation Conversation { get; set; } = null!;

    public AiMessageRole Role { get; set; }

    public string Text { get; set; } = null!;

    /// <summary>Bu turda çağrılan araçların adları (JSON dizi); kullanıcı satırında boş.</summary>
    public string? ToolNamesJson { get; set; }

    /// <summary>
    /// Yanıtın dayandığı girişim kimlikleri (JSON dizi). Yapılandırılmış olarak
    /// duruyor ki arayüz cevaptaki girişimlere doğrudan bağlantı verebilsin —
    /// metinden isim ayıklamak hem kırılgan hem uydurmaya açık.
    /// </summary>
    public string? StartupIdsJson { get; set; }

    /// <summary>
    /// Yanıtı model mi yerel plan mı üretti. <c>AssistantMode</c> enum'u
    /// Application katmanında yaşıyor ve Domain'in Application'a bağımlı olması
    /// yasak; enum'u Domain'e kopyalamak da aynı bilgiyi iki yerde tutup
    /// sessizce ayrışmalarına izin vermek olurdu. Bu yüzden mod burada
    /// enum'un adı olarak (<c>"Model"</c>/<c>"Local"</c>) saklanıyor: tek
    /// tanım Application'da kalıyor, veritabanındaki değer de psql'den
    /// okunduğunda anlamlı.
    /// </summary>
    public string? Mode { get; set; }

    public string? ModelName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
