using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Omni2FA.Core.Configuration;

namespace Omni2FA.AspNetCore.Internal;

internal static class SessionAuthorizationExtensions {
    /// <summary>
    /// Require the audience's host session on this endpoint. With no scheme or policy configured this is
    /// plain <c>RequireAuthorization()</c> — the host's default policy, as before audiences existed.
    /// Applied only to session-authenticated endpoints: <c>/challenge/*</c> runs before a session exists.
    /// </summary>
    public static TBuilder RequireOmni2FaSession<TBuilder>(this TBuilder builder, Omni2FaAudienceOptions audience) where TBuilder : IEndpointConventionBuilder {
        var schemes = audience.AuthenticationSchemes;
        var policy = audience.AuthorizationPolicy;
        if (string.IsNullOrWhiteSpace(schemes) && string.IsNullOrWhiteSpace(policy)) {
            builder.RequireAuthorization();
            return builder;
        }
        var authorize = new AuthorizeAttribute();
        if (!string.IsNullOrWhiteSpace(schemes)) {
            authorize.AuthenticationSchemes = schemes;
        }
        if (!string.IsNullOrWhiteSpace(policy)) {
            authorize.Policy = policy;
        }
        builder.RequireAuthorization(authorize);
        return builder;
    }
}
