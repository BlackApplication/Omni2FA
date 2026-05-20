namespace Omni2FA.Core.Results;

/// <summary>
/// Outcome of an operation that may fail. On failure, <see cref="ErrorCode"/> carries a stable
/// machine-readable code from <see cref="Errors.Omni2FaErrorCodes"/>.
/// </summary>
public class Result {
    /// <summary>True if the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>True if the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Stable error code on failure. Null on success.</summary>
    public string? ErrorCode { get; }

    /// <summary>Human-readable error message on failure. Null on success.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Optional structured error context on failure.</summary>
    public IReadOnlyDictionary<string, object?>? ErrorDetails { get; }

    /// <summary>Protected constructor — use <see cref="Success()"/> or <see cref="Failure(string, string?, IReadOnlyDictionary{string, object?}?)"/>.</summary>
    protected Result(bool isSuccess, string? errorCode, string? errorMessage, IReadOnlyDictionary<string, object?>? details) {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ErrorDetails = details;
    }

    /// <summary>Produce a successful result.</summary>
    public static Result Success() {
        return new Result(true, null, null, null);
    }

    /// <summary>Produce a failed result with the given error code and optional message / details.</summary>
    public static Result Failure(string errorCode, string? errorMessage = null, IReadOnlyDictionary<string, object?>? details = null) {
        return new Result(false, errorCode, errorMessage ?? errorCode, details);
    }
}
