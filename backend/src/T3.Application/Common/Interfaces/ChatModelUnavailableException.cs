namespace T3.Application.Common.Interfaces;

/// <summary>
/// Dil modeli sağlayıcısı <b>geçici olarak</b> yanıt vermedi: kota aşımı (429),
/// sağlayıcı tarafında 5xx, boş ya da hata taşıyan gövde. Yeniden denemeler
/// tükendikten sonra fırlatılır.
///
/// Neden ayrı bir tip: <see cref="HttpRequestException"/> olarak çıktığında bu
/// durum "beklenmeyen hata" ile aynı yola düşüyor ve kullanıcı HTTP 500 ile
/// referans numarası görüyordu — oysa burada hatalı olan bir şey yok, ücretsiz
/// katmanda 429 olağan bir cevap. Ayrı tip iki şeyi mümkün kılıyor:
/// <list type="bullet">
///   <item>çağıran taraf modelsiz yedek yola düşebiliyor (sohbet, kart özeti),</item>
///   <item>yedeği olmayan uçta hata 503'e eşlenebiliyor (bkz.
///   ExceptionHandlingMiddleware) — istemci "sonra tekrar dene" diyebilsin.</item>
/// </list>
///
/// Application katmanında duruyor çünkü iki taraf da burayı görüyor:
/// sağlayıcı adaptörleri (Infrastructure) fırlatır, use-case'ler yakalar.
/// </summary>
public sealed class ChatModelUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
