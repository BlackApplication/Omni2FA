using Microsoft.AspNetCore.Http;
using Omni2FA.Core.Dtos;
using Omni2FA.Core.Errors;
using Omni2FA.Core.Results;

namespace Omni2FA.AspNetCore.Extensions;

/// <summary>Maps <see cref="Result"/> / <see cref="Result{T}"/> to <see cref="IResult"/> responses.</summary>
internal static class ResultExtensions {
    public static IResult ToHttpResult(this Result result) {
        return result.IsSuccess ? Results.NoContent() : ErrorResult(result);
    }

    public static IResult ToHttpResult<T>(this Result<T> result) {
        return result.IsSuccess ? Results.Ok(result.Value) : ErrorResult(result);
    }

    private static IResult ErrorResult(Result result) {
        var code = result.ErrorCode!;
        var status = MapStatus(code);
        return Results.Json(new ErrorResponse {
            Code = code,
            Message = result.ErrorMessage ?? code,
            Details = result.ErrorDetails?.ToDictionary(kv => kv.Key, kv => kv.Value),
        }, statusCode: status);
    }

    private static int MapStatus(string code) {
        return code switch {
            Omni2FaErrorCodes.ValidationFailed => StatusCodes.Status400BadRequest,
            Omni2FaErrorCodes.InvalidCode or
            Omni2FaErrorCodes.PreAuthExpired or
            Omni2FaErrorCodes.PreAuthInvalid or
            Omni2FaErrorCodes.ChallengeConsumed or
            Omni2FaErrorCodes.WebAuthnVerificationFailed or
            Omni2FaErrorCodes.RecoveryCodeInvalid or
            Omni2FaErrorCodes.RecoveryCodeUsed => StatusCodes.Status401Unauthorized,
            Omni2FaErrorCodes.MethodNotFound or
            Omni2FaErrorCodes.ChallengeNotFound => StatusCodes.Status404NotFound,
            Omni2FaErrorCodes.TypeAlreadyEnrolled or
            Omni2FaErrorCodes.LastMethodProtected or
            Omni2FaErrorCodes.MaxMethodsReached => StatusCodes.Status409Conflict,
            Omni2FaErrorCodes.TooManyAttempts => StatusCodes.Status429TooManyRequests,
            _ => StatusCodes.Status500InternalServerError,
        };
    }
}
