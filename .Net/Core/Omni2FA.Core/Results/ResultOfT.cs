namespace Omni2FA.Core.Results;

/// <summary>
/// Outcome of an operation that returns a value on success. On failure, <see cref="Result.ErrorCode"/>
/// carries a stable machine-readable code and <see cref="Value"/> is <c>default</c>.
/// </summary>
/// <typeparam name="T">Type of the success value.</typeparam>
public class Result<T> : Result {
    /// <summary>Success value. <c>default</c> on failure.</summary>
    public T? Value { get; }

    private Result(bool isSuccess, T? value, string? errorCode, string? errorMessage, IReadOnlyDictionary<string, object?>? details)
        : base(isSuccess, errorCode, errorMessage, details) {
        Value = value;
    }

    /// <summary>Produce a successful result carrying <paramref name="value"/>.</summary>
    public static Result<T> Success(T value) {
        return new Result<T>(true, value, null, null, null);
    }

    /// <summary>Produce a failed result with the given error code and optional message / details.</summary>
    public static new Result<T> Failure(string errorCode, string? errorMessage = null, IReadOnlyDictionary<string, object?>? details = null) {
        return new Result<T>(false, default, errorCode, errorMessage ?? errorCode, details);
    }
}
