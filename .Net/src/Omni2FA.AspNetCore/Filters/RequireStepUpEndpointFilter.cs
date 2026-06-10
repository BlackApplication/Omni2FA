using Microsoft.AspNetCore.Http;

namespace Omni2FA.AspNetCore.Filters;

/// <summary>
/// Minimal-API endpoint filter that enforces step-up 2FA before the handler runs. Apply with the
/// <c>RequireStepUp()</c> route extension. Stateless — all dependencies resolve from the request scope,
/// so a single shared instance is reused across endpoints.
/// </summary>
internal sealed class RequireStepUpEndpointFilter : IEndpointFilter {
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) {
        var gate = await StepUpGate.EvaluateAsync(context.HttpContext).ConfigureAwait(false);
        if (gate.IsBlocked) {
            return Results.Json(gate.Error, statusCode: gate.StatusCode);
        }
        return await next(context).ConfigureAwait(false);
    }
}
