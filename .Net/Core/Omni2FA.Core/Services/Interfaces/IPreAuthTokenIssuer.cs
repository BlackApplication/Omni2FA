namespace Omni2FA.Core.Services.Interfaces;

/// <summary>
/// Issues and validates short-lived pre-auth tokens. The host application calls
/// <see cref="Issue"/> after verifying the user's password and returns the resulting
/// <see cref="PreAuthTokenInfo"/> to the frontend as part of the login response.
/// </summary>
public interface IPreAuthTokenIssuer {
    /// <summary>Mint a fresh pre-auth token for the given user, valid for the configured TTL.</summary>
    PreAuthTokenInfo Issue(Guid userId);

    /// <summary>
    /// Validate a previously-issued token. Returns the user id encoded in the token on success,
    /// or null if the token is missing, malformed, signed by an unknown key, or expired.
    /// </summary>
    Guid? ValidateAndGetUserId(string token);
}
