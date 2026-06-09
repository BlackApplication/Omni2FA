namespace Omni2FA.Core.Configuration;

/// <summary>
/// Controls where <c>POST /enroll/email/start</c> takes the OTP destination address from.
/// </summary>
public enum EmailEnrollmentAddressSource {
    /// <summary>
    /// Address is resolved from the current user's identity via
    /// <c>IUserContextAccessor.GetCurrentUserEmail()</c>; any address in the request body is ignored.
    /// Secure by default — the host cannot accidentally enroll an unverified, caller-supplied address,
    /// so every host gets ownership enforcement without writing per-endpoint glue.
    /// </summary>
    ClaimOnly,

    /// <summary>
    /// Address is taken from <c>EmailEnrollStartRequest.Email</c>. The host is responsible for supplying
    /// an authoritative, verified address. Use only when the enrollment target legitimately differs from
    /// the signed-in identity (e.g. SSO login where the OTP must reach a separate address).
    /// </summary>
    HostSupplied,
}
