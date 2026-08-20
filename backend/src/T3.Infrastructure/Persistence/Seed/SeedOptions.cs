namespace T3.Infrastructure.Persistence.Seed;

public class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>
    /// Tohum verinin yüklenip yüklenmeyeceği. Üretim ortamında hiçbir koşulda
    /// çalışmaz — <see cref="DevDataSeeder"/> ortamı ayrıca kontrol eder.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Demo kullanıcılarının şifresi. Yalnızca yerel geliştirme ve demo içindir;
    /// ortam değişkeniyle (T3_Seed__Password) değiştirilebilir.
    /// </summary>
    public string Password { get; set; } = "T3.Creathon!2026";
}
