namespace Omni2FA.Core.Results;

/// <summary>
/// Outcome of an operation that returns a value on success. On failure, <see cref="Result.ErrorCode"/>
/// carries a stable machine-readable code and <see cref="Value"/> is <c>default</c>.
/// </summary>
/// <typeparam name="T">Type of the success value.</typeparam>
public class Result<T> : Result {
    /// <summary><c>default</c> on failure.</summary>
    public T? Value { get; }

    private Result(bool isSuccess, T? value, string? errorCode, string? errorMessage, IReadOnlyDictionary<string, object?>? details)
        : base(isSuccess, errorCode, errorMessage, details) {
        Value = value;
    }

    public static Result<T> Success(T value) {
        return new Result<T>(true, value, null, null, null);
    }

    public static new Result<T> Failure(string errorCode, string? errorMessage = null, IReadOnlyDictionary<string, object?>? details = null) {
        return new Result<T>(false, default, errorCode, errorMessage ?? errorCode, details);
    }
}
