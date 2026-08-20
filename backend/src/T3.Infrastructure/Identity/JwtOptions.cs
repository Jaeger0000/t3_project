namespace T3.Infrastructure.Identity;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Yalnızca ortam değişkeninden gelir; repoda tutulmaz.</summary>
    public string Secret { get; set; } = null!;

    public string Issuer { get; set; } = "t3-ekosistem";
    public string Audience { get; set; } = "t3-ekosistem-api";

    /// <summary>Erişim jetonu ömrü (dakika). Kısa tutulur, refresh ile yenilenir.</summary>
    public int AccessTokenMinutes { get; set; } = 15;
}
