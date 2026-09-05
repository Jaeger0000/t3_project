namespace T3.Infrastructure.Identity;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Yalnızca ortam değişkeninden gelir; repoda tutulmaz.</summary>
    public string Secret { get; set; } = null!;

    public string Issuer { get; set; } = "t3-ekosistem";
    public string Audience { get; set; } = "t3-ekosistem-api";

    /// <summary>
    /// Erişim jetonu ömrü (dakika). Bir iş günü (480) yerine 60: yenileme
    /// jetonu hâlâ yok (o değişiklik ayrı bir karar — bkz. not), ama artık
    /// süre kısa olsun diye buna ihtiyaç da azaldı. Asıl "yetkiyi geri alma"
    /// garantisi bu süreden değil <c>IUserStateProvider</c>'ın istek başına
    /// canlı <c>IsActive</c>/<c>Role</c>/<c>SecurityStamp</c> kontrolünden
    /// geliyor: pasife alınan/rolü değişen/şifresi değişen kullanıcının eski
    /// jetonu 60 dakika değil bir sonraki istekte (en geç ~45 sn önbellek
    /// penceresinde) geçersiz olur (bkz. G-01, Guvenlik_Denetimi_ve_Iyilestirme_Plani.md).
    ///
    /// Yenileme jetonu bilinçli olarak bu turda eklenmedi: HttpOnly çerez +
    /// rotasyon + istemci tarafı sessiz yenileme akışı ayrı bir özellik ve
    /// Faz 1'in geri kalanından daha büyük bir yüzey. Kullanıcı 60 dakikada
    /// bir yeniden giriş yapar; bu bilinçli bir kısayoldur (Dalga 1'in 1.3
    /// maddesindeki "kısa yol seçildi" kararıyla aynı desen).
    /// </summary>
    public int AccessTokenMinutes { get; set; } = 60;
}
