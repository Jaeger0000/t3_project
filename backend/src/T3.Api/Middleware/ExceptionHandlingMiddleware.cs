using System.Net;
using FluentValidation;
using T3.Api.Http;

namespace T3.Api.Middleware;

/// <summary>
/// Beklenmeyen hataları tek noktada RFC 7807 benzeri bir gövdeye çevirir.
/// İstemciye yığın izi sızmaz; ayrıntı yalnızca sunucu loglarına yazılır.
/// </summary>
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "Doğrulama hatası: {Path}", context.Request.Path);
            // Referans yok: kullanıcının kendi düzeltebileceği bir hata.
            await WriteAsync(context, HttpStatusCode.BadRequest, "Doğrulama hatası",
                ex.Errors.Select(e => e.ErrorMessage).ToArray());
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Yetkisiz erişim denemesi: {Path}", context.Request.Path);
            await WriteAsync(context, HttpStatusCode.Forbidden, "Bu işlem için yetkiniz yok.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "İşlenmeyen hata: {Path}", context.Request.Path);
            // Referans = TraceIdentifier: RequestIdMiddleware bunu X-Request-Id
            // ile eşitledi, kullanıcı ekranda gördüğü kodu söyleyince log tek
            // sorguda bulunur.
            await WriteAsync(context, HttpStatusCode.InternalServerError,
                "Beklenmeyen bir hata oluştu.", referans: context.TraceIdentifier);
        }
    }

    private static async Task WriteAsync(
        HttpContext context, HttpStatusCode status, string title,
        string[]? errors = null, string? referans = null)
    {
        if (context.Response.HasStarted)
            return;

        // Clear() yalnızca gövdeyi değil, o ana kadar konan tüm başlıkları da
        // siler — RequestIdMiddleware'in koyduğu X-Request-Id dâhil. Referans
        // ile yanıt başlığı aynı kalsın diye TraceIdentifier'dan yeniden yazılır.
        context.Response.Clear();
        context.Response.StatusCode = (int)status;
        context.Response.Headers[RequestIdMiddleware.HeaderName] = context.TraceIdentifier;

        // WriteAsJsonAsync (JsonSerializer.Serialize değil): DI'daki JSON
        // seçenekleri (camelCase) burada da uygulansın — frontend tek bir
        // gövde şekli (küçük harfli alan adları) bekliyor.
        await context.Response.WriteAsJsonAsync(
            new ApiErrorBody((int)status, title, errors, referans));
    }
}
