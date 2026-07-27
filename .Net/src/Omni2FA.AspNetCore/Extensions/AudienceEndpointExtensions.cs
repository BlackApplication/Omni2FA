using Microsoft.AspNetCore.Builder;
using Omni2FA.AspNetCore.Routing;

namespace Omni2FA.AspNetCore.Extensions;

/// <summary>Route extension that tags the host's own endpoints with the audience they serve.</summary>
public static class AudienceEndpointExtensions {
    /// <summary>
    /// Mark this endpoint (or group) as serving <paramref name="audienceName"/>, so Omni2FA resolves the
    /// caller's subject in that audience's namespace. The minimal-API counterpart of
    /// <see cref="Omni2FaAudienceAttribute"/>; apply it to the group the audience's own endpoints already
    /// live in (e.g. the customer portal's <c>MapGroup("/api/portal")</c>).
    /// </summary>
    public static TBuilder WithOmni2FaAudience<TBuilder>(this TBuilder builder, string audienceName) where TBuilder : IEndpointConventionBuilder {
        ArgumentException.ThrowIfNullOrWhiteSpace(audienceName);
        builder.WithMetadata(new Omni2FaAudienceAttribute(audienceName));
        return builder;
    }
}
