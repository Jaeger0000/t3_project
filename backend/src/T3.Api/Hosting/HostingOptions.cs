namespace T3.Api.Hosting;

/// <summary>
/// Barındırma kararları: TLS zorunluluğu ve güvenilen vekil listesi.
///
/// İkisi de yapılandırmadan gelir çünkü aynı derleme hem geliştirme
/// makinesinde (düz HTTP, vekil yok) hem ters vekil arkasında (TLS önde,
/// istekler loopback'ten gelir) çalışıyor. Varsayılanlar geliştirme tarafını
/// tutar; üretim değerleri <c>docker-compose.prod.yml</c> içinde açıkça verilir
/// ve eksikse açılışta uyarı düşer.
/// </summary>
public sealed class HostingOptions
{
    public const string SectionName = "Hosting";

    /// <summary>
    /// HTTP isteklerini HTTPS'e yönlendir ve HSTS gönder. Uygulamanın kendisi
    /// TLS sonlandırmıyorsa (tipik kurulum: nginx/Traefik önde) bunu vekil
    /// yapar; o zaman bu bayrak <c>false</c> kalır ve yönlendirme öndedir.
    /// </summary>
    public bool RequireHttps { get; set; }

    /// <summary>
    /// <c>X-Forwarded-For</c>/<c>-Proto</c> başlıklarına güvenilecek vekillerin
    /// IP'leri. Boşsa yalnızca loopback güvenilir (ASP.NET varsayılanı).
    ///
    /// Liste bilinçli olarak "hepsine güven" seçeneği sunmuyor: denetim izine
    /// yazılan IP bu başlıktan geliyor ve herkesin yazabildiği bir başlığa
    /// güvenmek izi doğrudan yanıltıcı hâle getirir.
    /// </summary>
    public string[] TrustedProxies { get; set; } = [];

    /// <summary>
    /// Derlenmiş arayüzün sunulacağı kök. Boşsa <c>wwwroot</c> kullanılır.
    /// Konteynerde yayın çıktısı bu klasöre kopyalanıyor.
    /// </summary>
    public string? WebRoot { get; set; }
}
