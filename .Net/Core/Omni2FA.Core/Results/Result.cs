namespace Omni2FA.Core.Results;

/// <summary>
/// Outcome of an operation that may fail. On failure, <see cref="ErrorCode"/> carries a stable
/// machine-readable code from <see cref="Errors.Omni2FaErrorCodes"/>.
/// </summary>
public class Result {
    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    /// <summary>Null on success.</summary>
    public string? ErrorCode { get; }

    /// <summary>Null on success.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Null on success. Shape depends on <see cref="ErrorCode"/>.</summary>
    public IReadOnlyDictionary<string, object?>? ErrorDetails { get; }

    protected Result(bool isSuccess, string? errorCode, string? errorMessage, IReadOnlyDictionary<string, object?>? details) {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ErrorDetails = details;
    }

    public static Result Success() {
        return new Result(true, null, null, null);
    }

    public static Result Failure(string errorCode, string? errorMessage = null, IReadOnlyDictionary<string, object?>? details = null) {
        return new Result(false, errorCode, errorMessage ?? errorCode, details);
    }
}
