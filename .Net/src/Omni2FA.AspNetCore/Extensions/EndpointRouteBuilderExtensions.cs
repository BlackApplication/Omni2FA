using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Omni2FA.AspNetCore.Endpoints;
using Omni2FA.AspNetCore.Routing;
using Omni2FA.Core.Configuration;
using Omni2FA.Core.Services.Interfaces;

namespace Omni2FA.AspNetCore.Extensions;

/// <summary>Maps Omni2FA endpoints onto the host's routing pipeline.</summary>
public static class EndpointRouteBuilderExtensions {
    /// <summary>
    /// Map all Omni2FA endpoints for one audience, under that audience's route prefix (the default
    /// audience uses <see cref="AspNetCoreOptions.RoutePrefix"/>, default <c>/api/2fa</c>). Call after the
    /// authentication and authorization middleware. A host with separately authenticating populations
    /// calls this once per audience — see <see cref="Omni2FaAudienceOptions"/>.
    /// </summary>
    /// <param name="endpoints">The host's endpoint route builder.</param>
    /// <param name="audienceName">Audience to mount; null mounts the default audience.</param>
    /// <returns>
    /// The mounted group, so hosts can attach their own conventions. Note that anything chained here
    /// applies to <c>/challenge/*</c> as well — do not require a session on it, or login breaks; per-audience
    /// scheme and policy belong in <see cref="Omni2FaAudienceOptions"/>, which applies them only where a
    /// session exists.
    /// </returns>
    public static RouteGroupBuilder MapOmni2Fa(this IEndpointRouteBuilder endpoints, string? audienceName = null) {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<Omni2FaOptions>>().Value;
        var audience = endpoints.ServiceProvider.GetRequiredService<IOmni2FaAudienceRegistry>().Resolve(audienceName);
        var stepUp = options.StepUp;

        var group = endpoints.MapGroup(audience.RoutePrefix);
        group.WithMetadata(new Omni2FaAudienceAttribute(audience.Name));

        MethodsEndpoints.Map(group, audience, stepUp.RequireTwoFactorToRemoveMethod);
        EnrollTotpEndpoints.Map(group, audience, stepUp.RequireTwoFactorToEnroll);
        EnrollEmailEndpoints.Map(group, audience, stepUp.RequireTwoFactorToEnroll);
        EnrollWebAuthnEndpoints.Map(group, audience, stepUp.RequireTwoFactorToEnroll);
        ChallengeEndpoints.Map(group, audience);
        StepUpEndpoints.Map(group, audience);
        RecoveryCodesEndpoints.Map(group, audience, stepUp.RequireTwoFactorToRegenerateRecoveryCodes);

        return group;
    }
}
