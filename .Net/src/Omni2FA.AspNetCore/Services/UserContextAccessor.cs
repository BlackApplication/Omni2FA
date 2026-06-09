using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Omni2FA.AspNetCore.Services.Interfaces;
using Omni2FA.Core.Configuration;

namespace Omni2FA.AspNetCore.Services;

/// <summary>
/// Default <see cref="IUserContextAccessor"/> reading claims from <c>HttpContext.User</c>.
/// Looks up the configured <see cref="AspNetCoreOptions.UserIdClaim"/> first; falls back to
/// raw JWT <c>sub</c>/<c>email</c> claims so hosts that disable claim mapping
/// (<c>MapInboundClaims = false</c>) work out of the box.
/// </summary>
public class UserContextAccessor : IUserContextAccessor {
    private readonly IHttpContextAccessor _httpContext;
    private readonly AspNetCoreOptions _options;

    public UserContextAccessor(IHttpContextAccessor httpContext, IOptions<Omni2FaOptions> options) {
        _httpContext = httpContext;
        _options = options.Value.AspNetCore;
    }

    public virtual string GetCurrentUserId() {
        var value = FindClaimValue(_options.UserIdClaim, JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrWhiteSpace(value)) {
            throw new InvalidOperationException(
                $"No '{_options.UserIdClaim}' or 'sub' claim on the current principal. " +
                "Set Omni2FaOptions.AspNetCore.UserIdClaim if your host stores the user id under a different claim, " +
                "or override IUserContextAccessor via DI to read it from a custom source.");
        }
        return value;
    }

    public virtual string GetCurrentUserLabel() {
        var value = FindClaimValue(_options.UserLabelClaim, JwtRegisteredClaimNames.Email);
        return string.IsNullOrWhiteSpace(value) ? GetCurrentUserId() : value;
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

    /// <summary>Reads <paramref name="primaryClaim"/> from the current principal, falling back to <paramref name="fallbackClaim"/> (raw JWT claim for hosts with <c>MapInboundClaims = false</c>).</summary>
    private string? FindClaimValue(string primaryClaim, string fallbackClaim) {
        var ctx = _httpContext.HttpContext ?? throw new InvalidOperationException("No HttpContext available.");
        return ctx.User.FindFirst(primaryClaim)?.Value ?? ctx.User.FindFirst(fallbackClaim)?.Value;
    }
}
