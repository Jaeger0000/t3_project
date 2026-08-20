namespace T3.Application.Common.Results;

public enum ErrorKind
{
    None = 0,
    NotFound = 1,
    Validation = 2,
    Forbidden = 3,
    Conflict = 4
}

public sealed record Error(ErrorKind Kind, string Message)
{
    public static Error NotFound(string message) => new(ErrorKind.NotFound, message);
    public static Error Validation(string message) => new(ErrorKind.Validation, message);
    public static Error Forbidden(string message) => new(ErrorKind.Forbidden, message);
    public static Error Conflict(string message) => new(ErrorKind.Conflict, message);
}

/// <summary>
/// Handler dönüş tipi. İstisna fırlatmak yerine hatayı veri olarak taşır;
/// endpoint katmanı bunu HTTP durum koduna çevirir.
/// </summary>
public sealed class Result<T>
{
    private Result(T value) { Value = value; IsSuccess = true; }
    private Result(Error error) { Error = error; IsSuccess = false; }

    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }

    public static Result<T> Ok(T value) => new(value);
    public static Result<T> Fail(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Ok(value);
    public static implicit operator Result<T>(Error error) => Fail(error);
}
