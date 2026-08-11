namespace Aovias.Platform.Core;

/// <summary>
/// Generic operation result wrapper.
/// </summary>
public sealed class Result<T>
{
    public bool IsSuccess { get; }

    public T? Value { get; }

    public Error? Error { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        Error = error;
    }

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(Error error) => new(error);

    public static Result<T> Failure(string code, string message, string? detail = null)
        => new(Error.Create(code, message, detail));
}

/// <summary>
/// Non-generic operation result.
/// </summary>
public sealed class OperationResult
{
    public bool IsSuccess { get; }

    public Error? Error { get; }

    private OperationResult(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static OperationResult Success() => new(true, null);

    public static OperationResult Failure(Error error) => new(false, error);

    public static OperationResult Failure(string code, string message, string? detail = null)
        => new(false, Error.Create(code, message, detail));
}
