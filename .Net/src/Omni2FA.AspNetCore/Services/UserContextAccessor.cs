using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Omni2FA.AspNetCore.Routing;
using Omni2FA.AspNetCore.Services.Interfaces;
using Omni2FA.Core.Configuration;
using Omni2FA.Core.Services.Interfaces;

namespace Omni2FA.AspNetCore.Services;

/// <summary>
/// Default <see cref="IUserContextAccessor"/> reading claims from <c>HttpContext.User</c>.
/// Looks up the configured <see cref="AspNetCoreOptions.UserIdClaim"/> first; falls back to
/// raw JWT <c>sub</c>/<c>email</c> claims so hosts that disable claim mapping
/// (<c>MapInboundClaims = false</c>) work out of the box.
/// </summary>
public class UserContextAccessor : IUserContextAccessor {
    private readonly IHttpContextAccessor _httpContext;
    private readonly IOmni2FaAudienceRegistry _audiences;
    private readonly AspNetCoreOptions _options;

    public UserContextAccessor(IHttpContextAccessor httpContext, IOmni2FaAudienceRegistry audiences, IOptions<Omni2FaOptions> options) {
        _httpContext = httpContext;
        _audiences = audiences;
        _options = options.Value.AspNetCore;
    }

    public virtual string GetCurrentUserId() {
        return _audiences.ToSubject(CurrentAudienceName(), GetRawUserId());
    }

    public virtual string GetCurrentUserLabel() {
        var value = FindClaimValue(_options.UserLabelClaim, JwtRegisteredClaimNames.Email);
        // Falls back to the raw id, not the namespaced subject — the label is shown in authenticator apps.
        return string.IsNullOrWhiteSpace(value) ? GetRawUserId() : value;
    }

    public virtual string GetCurrentUserEmail() {
        var value = FindClaimValue(_options.UserEmailClaim, JwtRegisteredClaimNames.Email);
        if (string.IsNullOrWhiteSpace(value)) {
            throw new InvalidOperationException(
                $"No '{_options.UserEmailClaim}' or 'email' claim on the current principal. " +
                "Set Omni2FaOptions.AspNetCore.UserEmailClaim if your host stores the email under a different claim, " +
                "set Omni2FaOptions.AspNetCore.EmailEnrollmentAddressSource = HostSupplied to take the address from the request body, " +
                "or override IUserContextAccessor.GetCurrentUserEmail() to read it from a custom source.");
        }
        return value;
    }

    /// <summary>
    /// The user id exactly as the host's identity carries it, before the audience namespace is applied.
    /// Override this (rather than <see cref="GetCurrentUserId"/>) when the id lives outside the claims —
    /// namespacing then still happens for you.
    /// </summary>
    protected string GetRawUserId() {
        var value = FindClaimValue(_options.UserIdClaim, JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrWhiteSpace(value)) {
            throw new InvalidOperationException(
                $"No '{_options.UserIdClaim}' or 'sub' claim on the current principal. " +
                "Set Omni2FaOptions.AspNetCore.UserIdClaim if your host stores the user id under a different claim, " +
                "or override IUserContextAccessor via DI to read it from a custom source.");
        }
        return value;
    }

    /// <summary>
    /// Audience serving the current request, taken from endpoint metadata (<c>MapOmni2Fa("name")</c>,
    /// <c>[Omni2FaAudience]</c>, <c>WithOmni2FaAudience</c>). Null — the default audience — when the
    /// endpoint carries no such metadata, which is every endpoint in a single-population host.
    /// </summary>
    protected string? CurrentAudienceName() {
        return _httpContext.HttpContext?.GetEndpoint()?.Metadata.GetMetadata<IOmni2FaAudienceMetadata>()?.AudienceName;
    }

    /// <summary>Reads <paramref name="primaryClaim"/> from the current principal, falling back to <paramref name="fallbackClaim"/> (raw JWT claim for hosts with <c>MapInboundClaims = false</c>).</summary>
    private string? FindClaimValue(string primaryClaim, string fallbackClaim) {
        var ctx = _httpContext.HttpContext ?? throw new InvalidOperationException("No HttpContext available.");
        return ctx.User.FindFirst(primaryClaim)?.Value ?? ctx.User.FindFirst(fallbackClaim)?.Value;
    }
}
