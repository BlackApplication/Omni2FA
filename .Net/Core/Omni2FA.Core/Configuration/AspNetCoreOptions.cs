using System.Security.Claims;

namespace Omni2FA.Core.Configuration;

/// <summary>
/// Settings consumed by the ASP.NET Core adapter (<c>Omni2FA.AspNetCore</c>). Lives in Core so
/// the entire options tree binds from a single configuration section.
/// </summary>
public class AspNetCoreOptions {
    /// <summary>
    /// Primary claim used to identify the current user. Defaults to <c>ClaimTypes.NameIdentifier</c>, which
    /// matches ASP.NET's default JWT claim mapping. The lookup also falls back to raw JWT <c>sub</c> so
    /// hosts using <c>MapInboundClaims = false</c> work without further configuration.
    /// </summary>
    public string UserIdClaim { get; set; } = ClaimTypes.NameIdentifier;

    /// <summary>
    /// Primary claim used for the user-visible account label shown in authenticator apps. Defaults to
    /// <c>ClaimTypes.Email</c>; falls back to raw JWT <c>email</c>, then to the user id if neither is present.
    /// </summary>
    public string UserLabelClaim { get; set; } = ClaimTypes.Email;

    /// <summary>
    /// Claim carrying the user's email address, used as the OTP destination when
    /// <see cref="EmailEnrollmentAddressSource"/> is <see cref="Configuration.EmailEnrollmentAddressSource.ClaimOnly"/>.
    /// Defaults to <c>ClaimTypes.Email</c>; falls back to raw JWT <c>email</c>. Kept separate from
    /// <see cref="UserLabelClaim"/> on purpose — the label is cosmetic, whereas the OTP destination is
    /// security-sensitive and must point at an authoritative, verified address.
    /// </summary>
    public string UserEmailClaim { get; set; } = ClaimTypes.Email;

    /// <summary>
    /// Where <c>POST /enroll/email/start</c> takes the OTP destination address from. Defaults to
    /// <see cref="Configuration.EmailEnrollmentAddressSource.ClaimOnly"/> so the address is derived from
    /// the authenticated identity and ownership is enforced for every host by default. Set to
    /// <see cref="Configuration.EmailEnrollmentAddressSource.HostSupplied"/> to take it from the request body.
    /// </summary>
    public EmailEnrollmentAddressSource EmailEnrollmentAddressSource { get; set; } = EmailEnrollmentAddressSource.ClaimOnly;

    /// <summary>Mount point for all Omni2FA endpoints. Default <c>/api/2fa</c>.</summary>
    public string RoutePrefix { get; set; } = "/api/2fa";

    /// <summary>
    /// Separately authenticating user populations — staff and customers of the same application, each
    /// with their own login and their own identity table. Leave empty unless the host has more than one:
    /// the default audience (mounted at <see cref="RoutePrefix"/>, ids stored as-is) always exists and
    /// covers single-population hosts. Add an entry per additional population, then mount it with
    /// <c>MapOmni2Fa("name")</c> and tag the host's own endpoints for that population with
    /// <c>[Omni2FaAudience("name")]</c> / <c>WithOmni2FaAudience("name")</c>. See
    /// <see cref="Omni2FaAudienceOptions"/>.
    /// </summary>
    public IList<Omni2FaAudienceOptions> Audiences { get; } = [];

    /// <summary>How long a pending enrollment challenge remains valid before the user must restart the ceremony.</summary>
    public TimeSpan EnrollmentTtl { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Whether a user may remove their last active method (dropping to no 2FA). Default <c>true</c>.
    /// Set <c>false</c> for hosts that mandate 2FA — removing the last method then returns <c>409 LAST_METHOD_PROTECTED</c>.
    /// </summary>
    public bool AllowDisablingLastMethod { get; set; } = true;
}
