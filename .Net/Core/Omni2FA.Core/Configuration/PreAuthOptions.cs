namespace Omni2FA.Core.Configuration;

/// <summary>
/// Pre-auth token settings. The token is a short-lived JWT issued after password verification
/// and consumed by 2FA challenge endpoints.
/// </summary>
public class PreAuthOptions {
    /// <summary>
    /// HMAC-SHA256 signing key. Must be at least 32 characters for adequate security.
    /// Provide via configuration or environment variables — never hard-code.
    /// </summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Issuer (<c>iss</c>) claim for the JWT. Identifies which service minted the token.</summary>
    public string Issuer { get; set; } = "Omni2FA";

    /// <summary>Audience (<c>aud</c>) claim for the JWT. Identifies who the token is for.</summary>
    public string Audience { get; set; } = "Omni2FA-2FA-PreAuth";

    /// <summary>How long the token is valid after issuance. Default 5 minutes.</summary>
    public TimeSpan Ttl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>How long the verified-handoff token (issued after a challenge passes) stays valid. Default 2 minutes.</summary>
    public TimeSpan VerifiedTtl { get; set; } = TimeSpan.FromMinutes(2);
}
