using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Omni2FA.AspNetCore.Services.Interfaces;
using Omni2FA.Core.Configuration;

namespace Omni2FA.AspNetCore.Services;

/// <summary>Default <see cref="IUserContextAccessor"/> reading claims from <c>HttpContext.User</c>.</summary>
public class UserContextAccessor : IUserContextAccessor {
    private readonly IHttpContextAccessor _httpContext;
    private readonly AspNetCoreOptions _options;

    public UserContextAccessor(IHttpContextAccessor httpContext, IOptions<Omni2FaOptions> options) {
        _httpContext = httpContext;
        _options = options.Value.AspNetCore;
    }

    public string GetCurrentUserId() {
        var ctx = _httpContext.HttpContext ?? throw new InvalidOperationException("No HttpContext available.");
        var claim = ctx.User.FindFirst(_options.UserIdClaim);
        if (claim is null || string.IsNullOrWhiteSpace(claim.Value)) {
            throw new InvalidOperationException($"No '{_options.UserIdClaim}' claim on the current principal.");
        }
        return claim.Value;
    }

    public string GetCurrentUserLabel() {
        var ctx = _httpContext.HttpContext ?? throw new InvalidOperationException("No HttpContext available.");
        var labelClaim = ctx.User.FindFirst(_options.UserLabelClaim);
        return labelClaim is { Value.Length: > 0 } ? labelClaim.Value : GetCurrentUserId();
    }
}
