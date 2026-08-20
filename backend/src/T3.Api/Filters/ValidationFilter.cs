using FluentValidation;
using T3.Api.Http;

namespace T3.Api.Filters;

/// <summary>
/// Gövdeyi handler'a girmeden doğrular. MediatR ardışık düzeni olmadığı için
/// doğrulama uç nokta filtresinde çalışır: handler'lar doğrulayıcıyı elle
/// çağırmak zorunda kalmaz, kural unutulması mümkün olmaz.
/// </summary>
public sealed class ValidationFilter<TRequest>(IValidator<TRequest> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.Arguments.OfType<TRequest>().FirstOrDefault() is not { } request)
            return await next(context);

        var result = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);

        if (result.IsValid)
            return await next(context);

        return Results.Json(
            new ApiErrorBody(
                StatusCodes.Status400BadRequest,
                "Gönderilen veri geçersiz.",
                result.Errors.Select(e => e.ErrorMessage).Distinct().ToArray()),
            statusCode: StatusCodes.Status400BadRequest);
    }
}

public static class ValidationFilterExtensions
{
    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder) =>
        builder
            .AddEndpointFilter<ValidationFilter<TRequest>>()
            .ProducesValidationProblem();
}
