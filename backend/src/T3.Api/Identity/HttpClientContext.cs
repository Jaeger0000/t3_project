using T3.Application.Common.Interfaces;

namespace T3.Api.Identity;

/// <summary>
/// IClientContext'in HTTP uygulaması.
///
/// IP artık <c>X-Forwarded-For</c>'dan elle okunmuyor. Dalga 1'de öyleydi ve
/// değer bir <em>ipucuydu</em>: başlığı istemcinin kendisi de uydurabildiği için
/// denetim izine yazdığımız adres kanıt sayılamazdı. Dalga 2'de iş
/// <c>UseForwardedHeaders</c>'a devredildi — o katman başlığı yalnızca
/// <c>Hosting:TrustedProxies</c> listesindeki vekiller adına kabul edip
/// <c>RemoteIpAddress</c>'i düzeltiyor. Burada tek bir kaynağı okumak, "hangi
/// değere güveniyoruz" sorusunun cevabını tek yerde bırakıyor.
/// </summary>
public sealed class HttpClientContext(IHttpContextAccessor accessor) : IClientContext
{
    public string? IpAddress =>
        Trim(accessor.HttpContext?.Connection.RemoteIpAddress?.ToString(), 64);

    public string? UserAgent =>
        Trim(accessor.HttpContext?.Request.Headers.UserAgent.FirstOrDefault(), 256);

    private static string? Trim(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Length <= maxLength ? value : value[..maxLength];
}
