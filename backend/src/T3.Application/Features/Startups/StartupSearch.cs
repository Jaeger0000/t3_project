using System.Linq.Expressions;
using T3.Application.Common.Text;
using T3.Domain.Startups;

namespace T3.Application.Features.Startups;

/// <summary>
/// Girişim arama yüklemleri. Üç dilim (liste, CSV aktarımı, pano istatistiği)
/// aynı filtreyi kullanıyor; kural burada tek kopya duruyor.
///
/// Aksan katlaması iki tarafta da yapılmak zorunda: yalnızca terimi katlamak
/// ("saglik") kolonu ("Sağlık") olduğu gibi bırakır ve arama hiçbir zaman
/// eşleşmez. Kolon tarafındaki katlama SQL <c>replace()</c> zincirine dönüşen
/// <c>string.Replace</c> çağrılarıyla yapılıyor — <c>unaccent</c> uzantısı ya
/// da <c>ILIKE</c> sağlayıcıya özel olurdu, Application katmanında yasak.
///
/// Zincir her kolon için yeniden yazılıyor çünkü EF, gövdesi başka bir metotta
/// duran çağrıyı (<c>SearchText.Fold</c> gibi) SQL'e çeviremez: ifade ağacının
/// içinde durmak zorunda. Tekrarın bedeli bu dosyada kalıyor.
/// </summary>
public static class StartupSearch
{
    /// <summary>Ad, ürün açıklaması ya da şehirde serbest metin araması.</summary>
    public static Expression<Func<Startup, bool>> Matches(string rawTerm)
    {
        var term = SearchText.Fold(rawTerm);

        return s =>
            s.Name.ToLower()
                .Replace("ç", "c").Replace("ğ", "g").Replace("ı", "i")
                .Replace("ö", "o").Replace("ş", "s").Replace("ü", "u")
                .Contains(term)
            || (s.ProductDescription != null && s.ProductDescription.ToLower()
                .Replace("ç", "c").Replace("ğ", "g").Replace("ı", "i")
                .Replace("ö", "o").Replace("ş", "s").Replace("ü", "u")
                .Contains(term))
            || (s.City != null && s.City.ToLower()
                .Replace("ç", "c").Replace("ğ", "g").Replace("ı", "i")
                .Replace("ö", "o").Replace("ş", "s").Replace("ü", "u")
                .Contains(term));
    }

    /// <summary>
    /// Şehir eşitliği. Açılırdan seçilen değer de katlanıyor: "Şanlıurfa"
    /// seçeneği ile elle yazılmış "sanliurfa" aynı kaydı vermeli.
    /// </summary>
    public static Expression<Func<Startup, bool>> InCity(string rawCity)
    {
        var city = SearchText.Fold(rawCity);

        return s => s.City != null && s.City.ToLower()
            .Replace("ç", "c").Replace("ğ", "g").Replace("ı", "i")
            .Replace("ö", "o").Replace("ş", "s").Replace("ü", "u") == city;
    }
}
