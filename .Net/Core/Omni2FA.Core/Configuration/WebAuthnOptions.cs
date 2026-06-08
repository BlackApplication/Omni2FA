namespace Omni2FA.Core.Configuration;

/// <summary>
/// WebAuthn relying-party settings. <see cref="RelyingPartyId"/> and <see cref="Origins"/> must match
/// the site users actually visit — see <c>docs/FLOWS.md</c> "Common deployment gotchas" (HTTPS off-localhost).
/// </summary>
public class WebAuthnOptions {
    /// <summary>
    /// Relying-party id — the registrable domain, e.g. <c>localhost</c> in dev or <c>app.example.com</c>
    /// in production. Do not include scheme or port. Credentials are bound to this value.
    /// </summary>
    public string RelyingPartyId { get; set; } = "localhost";

    /// <summary>Human-readable relying-party name shown by some authenticators.</summary>
    public string RelyingPartyName { get; set; } = "Omni2FA";

    /// <summary>
    /// Full origins allowed to complete ceremonies, e.g. <c>http://localhost:5173</c> or
    /// <c>https://app.example.com</c>. Must include scheme and port.
    /// </summary>
    public IList<string> Origins { get; set; } = new List<string> { "http://localhost:5173" };

    /// <summary>Maximum WebAuthn credentials a single user may enrol.</summary>
    public int MaxCredentialsPerUser { get; set; } = 3;
}
