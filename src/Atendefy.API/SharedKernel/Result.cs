namespace Atendefy.API.SharedKernel;

public class Result
{
    public bool IsSuccess { get; protected init; }
    public string? Error { get; protected init; }

    public static Result Ok() => new() { IsSuccess = true };
    public static Result Fail(string error) => new() { IsSuccess = false, Error = error };
}

public class Result<T> : Result
{
    public T? Value { get; private init; }

    public static Result<T> Ok(T value) => new() { IsSuccess = true, Value = value };
    public new static Result<T> Fail(string error) => new() { IsSuccess = false, Error = error };
}
