using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Omni2FA.AspNetCore.Services.Interfaces;
using Omni2FA.Core.Configuration;
using Omni2FA.Core.Enums;
using Omni2FA.Core.Services.Interfaces;

namespace Omni2FA.AspNetCore.Filters;

/// <summary>
/// Shared step-up decision used by both the minimal-API endpoint filter and the MVC action filter.
/// Resolves the current user and the presented step-up header, asks <see cref="IStepUpEvaluator"/> for a
/// verdict, and — when blocked — builds the <c>STEP_UP_REQUIRED</c> envelope describing how to confirm.
/// </summary>
internal static class StepUpGate {
    /// <summary>Evaluate the request. Returns a blocked result to short-circuit, or <see cref="StepUpGateResult.Allowed"/> to proceed.</summary>
    public static async Task<StepUpGateResult> EvaluateAsync(HttpContext http) {
        var services = http.RequestServices;
        var options = services.GetRequiredService<IOptions<Omni2FaOptions>>().Value;
        var user = services.GetRequiredService<IUserContextAccessor>();
        var evaluator = services.GetRequiredService<IStepUpEvaluator>();

        var userId = user.GetCurrentUserId();
        var header = http.Request.Headers[options.StepUp.HeaderName].ToString();
        var token = string.IsNullOrWhiteSpace(header) ? null : header.Trim();

        var verdict = await evaluator.EvaluateAsync(userId, token, http.RequestAborted).ConfigureAwait(false);
        if (verdict != StepUpVerdict.Required) {
            return StepUpGateResult.Allowed;
        }

        // Blocked: tell the frontend which methods it can confirm with and where to do it.
        var methodsService = services.GetRequiredService<ITwoFactorMethodService>();
        var methods = await methodsService.ListAsync(userId, http.RequestAborted).ConfigureAwait(false);
        var stepUpPath = $"{options.AspNetCore.RoutePrefix.TrimEnd('/')}/stepup";
        return StepUpGateResult.Blocked(methods, stepUpPath);
    }
}
