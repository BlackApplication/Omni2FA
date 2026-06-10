using Microsoft.AspNetCore.Http;
using Omni2FA.Core.Dtos;
using Omni2FA.Core.Errors;

namespace Omni2FA.AspNetCore.Filters;

/// <summary>Result of <see cref="StepUpGate"/>: either allow the action or short-circuit with an error envelope.</summary>
internal sealed class StepUpGateResult {
    public bool IsBlocked { get; }
    public ErrorResponse? Error { get; }
    public int StatusCode { get; }

    private StepUpGateResult(bool isBlocked, ErrorResponse? error, int statusCode) {
        IsBlocked = isBlocked;
        Error = error;
        StatusCode = statusCode;
    }

    public static readonly StepUpGateResult Allowed = new(false, null, 0);

    public static StepUpGateResult Blocked(IReadOnlyList<TwoFactorMethodDto> methods, string stepUpPath) {
        var error = new ErrorResponse {
            Code = Omni2FaErrorCodes.StepUpRequired,
            Message = "Two-factor confirmation is required for this action.",
            Details = new Dictionary<string, object?> {
                ["availableMethods"] = methods,
                ["stepUpPath"] = stepUpPath,
            },
        };
        return new StepUpGateResult(true, error, StatusCodes.Status403Forbidden);
    }
}
