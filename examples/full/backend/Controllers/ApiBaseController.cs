using Microsoft.AspNetCore.Mvc;
using Omni2FA.Core.Results;

namespace Example.Backend.Controllers;

/// <summary>Base controller exposing <see cref="HandleResult{T}"/> — maps a <see cref="Result"/> envelope to an HTTP response.</summary>
[ApiController]
[Route("[controller]")]
public abstract class ApiBaseController : ControllerBase {
    protected IActionResult HandleResult<T>(Result<T> result) {
        if (result.IsSuccess) {
            return Ok(result.Value);
        }
        return Problem(result.ErrorMessage, statusCode: StatusFromCode(result.ErrorCode), title: result.ErrorCode);
    }

    protected IActionResult HandleResult(Result result) {
        if (result.IsSuccess) {
            return NoContent();
        }
        return Problem(result.ErrorMessage, statusCode: StatusFromCode(result.ErrorCode), title: result.ErrorCode);
    }

    private static int StatusFromCode(string? code) {
        return code switch {
            "INVALID_INPUT" => StatusCodes.Status400BadRequest,
            "INVALID_CREDENTIALS" => StatusCodes.Status401Unauthorized,
            "EMAIL_TAKEN" => StatusCodes.Status409Conflict,
            "USER_NOT_FOUND" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError,
        };
    }
}
