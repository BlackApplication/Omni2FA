using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Omni2FA.AspNetCore.Filters;

/// <summary>
/// Enforces step-up 2FA on an MVC controller or action — the <c>[Authorize]</c>-style entry point for
/// protecting sensitive operations (change password, view recovery codes, remove a method). If the user
/// has an active 2FA method and has not presented a valid, single-use step-up token, the request is
/// short-circuited with <c>403 STEP_UP_REQUIRED</c>; if the user has no 2FA enrolled, it passes through.
/// Apply on top of normal authorization — it confirms a fresh 2FA, it does not authenticate.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RequireTwoFactorAttribute : Attribute, IAsyncActionFilter {
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
        var gate = await StepUpGate.EvaluateAsync(context.HttpContext).ConfigureAwait(false);
        if (gate.IsBlocked) {
            context.Result = new JsonResult(gate.Error) { StatusCode = gate.StatusCode };
            return;
        }
        await next().ConfigureAwait(false);
    }
}
