namespace T3.Infrastructure.Identity;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Yalnızca ortam değişkeninden gelir; repoda tutulmaz.</summary>
    public string Secret { get; set; } = null!;

    public string Issuer { get; set; } = "t3-ekosistem";
    public string Audience { get; set; } = "t3-ekosistem-api";

    /// <summary>
    /// Erişim jetonu ömrü (dakika). Varsayılan bir iş günü (8 saat).
    ///
    /// 15 dakikaydı ve yenileme jetonu <em>yoktu</em>: kullanıcı çalışırken
    /// sessizce atılıyordu. Doğru çözüm yenileme jetonunu HttpOnly çereze
    /// koymak ama o değişiklik jetonun tamamını çereze taşımaya bağlı (Dalga 2)
    /// ve tek origin kararını bekliyor. O gelene kadar bilinçli takas: tek
    /// jeton, iş günü kadar ömür, süre dolmadan önce arayüzde uyarı.
    /// </summary>
    public int AccessTokenMinutes { get; set; } = 480;
}
