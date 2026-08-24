using T3.Application.Common.Interfaces;

namespace T3.Api.Identity;

/// <summary>
/// IClientContext'in HTTP uygulaması.
///
/// <c>X-Forwarded-For</c> yalnızca <em>ilk</em> değeri alacak şekilde okunuyor:
/// zincirin sonraki halkaları vekiller tarafından eklenir, istemcinin kendisi
/// başlığı uydurabildiği için bu değer kanıt değil ipucudur — denetim izinde
/// böyle yorumlanmalı. Ters vekil devreye girdiğinde
/// <c>ForwardedHeadersOptions</c> ile bu okuma güvenilir hâle getirilecek
/// (Dalga 2, üretim dağıtımı).
/// </summary>
public sealed class HttpClientContext(IHttpContextAccessor accessor) : IClientContext
{
    public string? IpAddress
    {
        get
        {
            var context = accessor.HttpContext;

            if (context is null)
                return null;

            var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(forwarded))
                return Trim(forwarded.Split(',')[0].Trim(), 64);

            return Trim(context.Connection.RemoteIpAddress?.ToString(), 64);
        }
    }

    public string? UserAgent =>
        Trim(accessor.HttpContext?.Request.Headers.UserAgent.FirstOrDefault(), 256);

    private static string? Trim(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Length <= maxLength ? value : value[..maxLength];
}
