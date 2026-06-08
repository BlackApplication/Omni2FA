namespace Omni2FA.Core.Services.WebAuthn;

/// <summary>Identity passed to the authenticator when creating a credential. All fields are caller-visible labels except <see cref="UserId"/>, which becomes the user handle.</summary>
public class WebAuthnUserInfo {
    /// <summary>Host's user id — becomes the WebAuthn user handle (encoded to bytes by the ceremony service).</summary>
    public required string UserId { get; init; }

    /// <summary>Account name shown by the authenticator / passkey manager (typically the email).</summary>
    public required string Name { get; init; }

    /// <summary>Human-friendly display name.</summary>
    public required string DisplayName { get; init; }
}
