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

    public string GetCurrentUserId() {
        var ctx = _httpContext.HttpContext ?? throw new InvalidOperationException("No HttpContext available.");
        var value = ctx.User.FindFirst(_options.UserIdClaim)?.Value
            ?? ctx.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrWhiteSpace(value)) {
            throw new InvalidOperationException(
                $"No '{_options.UserIdClaim}' or 'sub' claim on the current principal. " +
                "Set Omni2FaOptions.AspNetCore.UserIdClaim if your host stores the user id under a different claim, " +
                "or override IUserContextAccessor via DI to read it from a custom source.");
        }
        return value;
    }

    public string GetCurrentUserLabel() {
        var ctx = _httpContext.HttpContext ?? throw new InvalidOperationException("No HttpContext available.");
        var value = ctx.User.FindFirst(_options.UserLabelClaim)?.Value
            ?? ctx.User.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
        return string.IsNullOrWhiteSpace(value) ? GetCurrentUserId() : value;
    }
}
