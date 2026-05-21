using Microsoft.AspNetCore.Http;
using Omni2FA.AspNetCore.Internal;
using Omni2FA.Core.Dtos;
using Omni2FA.Core.Errors;
using Omni2FA.Core.Services.Interfaces;

namespace Omni2FA.AspNetCore.Filters;

/// <summary>
/// Validates the Bearer pre-auth token on <c>/challenge/*</c> endpoints. On success, stores
/// the decoded userId on <c>HttpContext.Items</c> for the endpoint handler. On failure, short-circuits
/// with an <see cref="ErrorResponse"/> envelope.
/// </summary>
internal sealed class PreAuthFilter : IEndpointFilter {
    private const string _bearerPrefix = "Bearer ";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) {
        var http = context.HttpContext;
        var header = http.Request.Headers.Authorization.ToString();
        if (!header.StartsWith(_bearerPrefix, StringComparison.OrdinalIgnoreCase)) {
            return PreAuthError(Omni2FaErrorCodes.PreAuthInvalid, "Missing or malformed Authorization header.");
        }
        var token = header[_bearerPrefix.Length..].Trim();

        if (http.RequestServices.GetService(typeof(IPreAuthTokenIssuer)) is not IPreAuthTokenIssuer issuer) {
            return PreAuthError(Omni2FaErrorCodes.PreAuthInvalid, "Pre-auth token issuer is not configured.");
        }

        var userId = issuer.ValidateAndGetUserId(token);
        if (userId is null) {
            return PreAuthError(Omni2FaErrorCodes.PreAuthInvalid, "Pre-auth token is missing, malformed, or expired.");
        }

        http.Items[PreAuthContextItems.UserId] = userId;
        return await next(context).ConfigureAwait(false);
    }

    private static IResult PreAuthError(string code, string message) {
        return Results.Json(new ErrorResponse { Code = code, Message = message }, statusCode: StatusCodes.Status401Unauthorized);
    }
}
