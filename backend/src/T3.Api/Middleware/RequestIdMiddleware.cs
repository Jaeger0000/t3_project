using Serilog.Context;

namespace T3.Api.Middleware;

/// <summary>
/// Kullanıcının ekranda gördüğü kısa referansla logdaki satırı aynı kimliğe
/// bağlar. Gelen <c>X-Request-Id</c> başlığı varsa ve biçimi güvenliyse onu
/// kullanır, yoksa üretir; yanıta aynı başlıkla geri koyar.
///
/// <see cref="HttpContext.TraceIdentifier"/> bilinçli olarak bu değere
/// eşitleniyor: <c>ExceptionHandlingMiddleware</c> ve istek özet logu aynı
/// kimliği kullansın, <c>ApiErrorBody.Referans</c> ile <c>X-Request-Id</c>
/// yanıt başlığı birebir aynı çıksın.
/// </summary>
public sealed class RequestIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Request-Id";
    private const int MaxLength = 64;

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = Resolve(context.Request.Headers[HeaderName].ToString());

        context.TraceIdentifier = requestId;
        context.Response.Headers[HeaderName] = requestId;

        using (LogContext.PushProperty("IstekId", requestId))
        {
            await next(context);
        }
    }

    /// <summary>
    /// Gelen değere doğrudan güvenilmez: uzunluk ve karakter sınırı uygulanır,
    /// yoksa istemci loga istediğini yazdırabilir (log injection).
    /// </summary>
    private static string Resolve(string? incoming)
    {
        if (!string.IsNullOrWhiteSpace(incoming)
            && incoming.Length <= MaxLength
            && incoming.All(c => char.IsLetterOrDigit(c) || c is '-' or '_'))
        {
            return incoming;
        }

        return Guid.NewGuid().ToString("n");
    }
}
