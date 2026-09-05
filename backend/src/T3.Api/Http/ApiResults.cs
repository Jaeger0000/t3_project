using T3.Application.Common.Results;

namespace T3.Api.Http;

/// <summary>
/// Hata gövdesi. Şekli ExceptionHandlingMiddleware ve ValidationFilter ile
/// birebir aynı: frontend tek bir hata biçimi ayrıştırır.
///
/// <see cref="Referans"/> yalnızca beklenmeyen (500) hatalarda dolar — bkz.
/// ExceptionHandlingMiddleware. Doğrulama hatası kullanıcının kendisinin
/// düzeltebileceği bir şey; referans numarası orada gürültü olur.
/// </summary>
public sealed record ApiErrorBody(int Status, string Title, string[]? Errors = null, string? Referans = null);

/// <summary>
/// Handler'ın <see cref="Result{T}"/> dönüşünü HTTP'ye çevirir. Uç noktalar
/// durum kodu seçmez — eşleme burada tek yerde durur, böylece aynı hata türü
/// her uçta aynı kodu üretir.
/// </summary>
public static class ApiResults
{
    public static IResult ToHttp<T>(this Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error!);

    public static IResult ToCreated<T>(this Result<T> result, Func<T, string> location) =>
        result.IsSuccess
            ? Results.Created(location(result.Value!), result.Value)
            : Problem(result.Error!);

    public static IResult ToNoContent<T>(this Result<T> result) =>
        result.IsSuccess ? Results.NoContent() : Problem(result.Error!);

    public static IResult Problem(Error error)
    {
        var status = error.Kind switch
        {
            ErrorKind.NotFound => StatusCodes.Status404NotFound,
            ErrorKind.Validation => StatusCodes.Status400BadRequest,
            ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
            ErrorKind.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Json(new ApiErrorBody(status, error.Message), statusCode: status);
    }
}
