using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Omni2FA.AspNetCore.Filters;

namespace Omni2FA.AspNetCore.Extensions;

/// <summary>Route extension that enforces step-up 2FA on minimal-API endpoints and groups.</summary>
public static class StepUpEndpointExtensions {
    /// <summary>
    /// Require a valid, single-use step-up token on this endpoint (or group). The minimal-API counterpart
    /// of <see cref="RequireTwoFactorAttribute"/>: enrolled users must confirm 2FA, users without 2FA pass
    /// through. Chain after <c>RequireAuthorization()</c> — step-up confirms identity freshness, it does
    /// not authenticate.
    /// </summary>
    public static TBuilder RequireStepUp<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder {
        return builder.AddEndpointFilter<TBuilder, RequireStepUpEndpointFilter>();
    }
}
