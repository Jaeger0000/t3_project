using System.Linq.Expressions;
using T3.Domain.Assistant;

namespace T3.Application.Features.Assistant.Chat;

/// <summary>
/// Sohbet satırlarının tek yetki noktası. Kural basit ve istisnasız: bir sohbeti
/// yalnızca sahibi görür — SuperAdmin dahil hiçbir rol için "başkasının
/// sohbetini oku" gerekçesi yok, çünkü sohbet metni kullanıcının kendi serbest
/// metni ve hiçbir formun sormadığı kişisel veriyi içerebilir.
///
/// Süzgeç ayrı bir yerde duruyor ki üç dilim (yazma, liste, tek kayıt) aynı
/// koşulu ayrı ayrı yazmasın; biri unutulursa sızıntı sessiz olurdu.
/// </summary>
public static class ConversationAccess
{
    /// <summary>Çağıranın bütün sohbetleri.</summary>
    public static Expression<Func<AiConversation, bool>> OwnedBy(Guid userId) =>
        c => c.OwnerUserId == userId;

    /// <summary>
    /// Çağıranın tek sohbeti. Sahiplik koşulu kimlik koşuluyla <b>aynı</b>
    /// sorguda: önce satırı bulup sonra sahibini kontrol etmek, "kayıt var ama
    /// senin değil" ile "kayıt yok" durumlarını ayırt edilebilir kılardı.
    /// </summary>
    public static Expression<Func<AiConversation, bool>> Owned(Guid conversationId, Guid userId) =>
        c => c.Id == conversationId && c.OwnerUserId == userId;
}
