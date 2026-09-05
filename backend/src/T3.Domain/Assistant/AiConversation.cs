using T3.Domain.Common;

namespace T3.Domain.Assistant;

/// <summary>
/// Sunucuda saklanan AI sohbeti. Çok turlu soru-cevabın bağlamı istemcide değil
/// burada duruyor: bağlamı tarayıcıya bırakmak, geçmişi istemcinin kurgulamasına
/// izin vermek demekti — kullanıcı hiç sorulmamış bir turu "sorulmuş" gibi
/// gönderebilirdi.
///
/// <b>Soft delete yok.</b> Diğer varlıklarda pasife alma denetim izinin
/// bütünlüğü için var; sohbet metni ise serbest metin, yani kişisel veri
/// taşıyabilen bir kayıt. KVKK saklama süresi dolduğunda gerçekten silinmeli,
/// bayrakla gizlenip veritabanında kalmamalı
/// (bkz. <c>RetentionCleanupService.ConversationRetention</c>).
/// </summary>
public class AiConversation : Entity, IAuditable
{
    /// <summary>
    /// Sohbetin sahibi. Navigasyon özelliği bilinçli olarak yok: sohbet
    /// dilimleri yalnızca "bu satır çağıranın mı" sorusunu soruyor, kullanıcının
    /// kendisini hiç okumuyor. Navigasyon eklemek, kişisel veri taşıyan User
    /// satırını istemeden yükleyip yanıta sızdırmanın kolay yolu olurdu.
    /// </summary>
    public Guid OwnerUserId { get; set; }

    /// <summary>Sohbet listesinde görünen başlık; ilk sorudan türetilir.</summary>
    public string Title { get; set; } = null!;

    /// <summary>Liste sıralamasının anahtarı — son konuşulan sohbet en üstte.</summary>
    public DateTimeOffset LastMessageAt { get; set; }

    public ICollection<AiConversationMessage> Messages { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
