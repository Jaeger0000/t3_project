namespace T3.Infrastructure.Persistence.Seed;

public class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>
    /// Tohum verinin yüklenip yüklenmeyeceği. Üretim ortamında hiçbir koşulda
    /// çalışmaz — <see cref="DevDataSeeder"/> ortamı ayrıca kontrol eder.
    /// Varsayılan **kapalı**: güvenlik varsayılanları kapalı başlamalı, açık
    /// tutulması gereken ortam (geliştirme) bunu appsettings.Development.json'da
    /// açıkça belirtir (bkz. G-14, Guvenlik_Denetimi_ve_Iyilestirme_Plani.md).
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Demo kullanıcılarının şifresi. Sabit bir varsayılanı **yok** — herkesin
    /// bildiği bir demo şifresinin depoda sabit durması kendi başına bir sır
    /// sızıntısıdır. Ortam değişkeniyle (T3_Seed__Password) verilmezse tohumlama
    /// açıkça hata verir.
    /// </summary>
    public string? Password { get; set; }
}
