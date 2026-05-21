using System.Security.Claims;

namespace Omni2FA.Core.Configuration;

/// <summary>
/// Settings consumed by the ASP.NET Core adapter (<c>Omni2FA.AspNetCore</c>). Lives in Core so
/// the entire options tree binds from a single configuration section.
/// </summary>
public class AspNetCoreOptions {
    /// <summary>Claim read from <c>HttpContext.User</c> to identify the current user. Host overrides if its userId sits in a non-standard claim.</summary>
    public string UserIdClaim { get; set; } = ClaimTypes.NameIdentifier;

    /// <summary>Claim read from <c>HttpContext.User</c> for the user-visible account label shown in authenticator apps. Falls back to userId if missing.</summary>
    public string UserLabelClaim { get; set; } = ClaimTypes.Email;

    /// <summary>Mount point for all Omni2FA endpoints. Default <c>/api/2fa</c>.</summary>
    public string RoutePrefix { get; set; } = "/api/2fa";

    /// <summary>How long a pending enrollment challenge remains valid before the user must restart the ceremony.</summary>
    public TimeSpan EnrollmentTtl { get; set; } = TimeSpan.FromMinutes(10);
}
