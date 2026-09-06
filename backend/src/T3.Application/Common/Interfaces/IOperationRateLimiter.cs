namespace T3.Application.Common.Interfaces;

/// <summary>
/// Kullanıcı başına işlem sayacı — HTTP hız sınırlama ara katmanının
/// göremediği durumlar için: aynı uçtan (ör. sohbet, MCP) geçen ama farklı
/// maliyet/tehdit profiline sahip bir işlem (ör. kütlesel veri dışa aktarma
/// aracı), o uca zaten uygulanmış olan gevşek kotayı miras alır, kendi
/// sıkı kotasını almaz. Bu arayüz o eksik katmanı doldurur.
/// </summary>
public interface IOperationRateLimiter
{
    /// <summary>
    /// <paramref name="key"/>+<paramref name="userId"/> bölümü için bu
    /// pencerede hâlâ hakkı var mı; varsa sayaç artırılır. Sınıra
    /// ulaşıldıysa sayaç artmaz, tekrar denemek pencere kapanana kadar
    /// başarısız kalır.
    /// </summary>
    bool TryConsume(string key, Guid userId, int limit, TimeSpan window);
}
