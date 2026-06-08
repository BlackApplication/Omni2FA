using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Omni2FA.AspNetCore.Endpoints;
using Omni2FA.Core.Configuration;

namespace Omni2FA.AspNetCore.Extensions;

/// <summary>Maps Omni2FA endpoints onto the host's routing pipeline.</summary>
public static class EndpointRouteBuilderExtensions {
    /// <summary>
    /// Map all Omni2FA endpoints under the prefix configured in <see cref="AspNetCoreOptions.RoutePrefix"/>
    /// (default <c>/api/2fa</c>). Call once in the application pipeline, after authentication and
    /// authorization middleware.
    /// </summary>
    public static IEndpointRouteBuilder MapOmni2Fa(this IEndpointRouteBuilder endpoints) {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<Omni2FaOptions>>().Value;
        var group = endpoints.MapGroup(options.AspNetCore.RoutePrefix);

        MethodsEndpoints.Map(group);
        EnrollTotpEndpoints.Map(group);
        EnrollEmailEndpoints.Map(group);
        EnrollWebAuthnEndpoints.Map(group);
        ChallengeEndpoints.Map(group);

        return endpoints;
    }
}
