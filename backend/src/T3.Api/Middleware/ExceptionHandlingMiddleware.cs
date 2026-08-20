using System.Net;
using System.Text.Json;
using FluentValidation;

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
            await WriteAsync(context, HttpStatusCode.InternalServerError,
                "Beklenmeyen bir hata oluştu.");
        }
    }

    private static async Task WriteAsync(
        HttpContext context, HttpStatusCode status, string title, string[]? errors = null)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = (int)status,
            title,
            errors
        }));
    }
}
