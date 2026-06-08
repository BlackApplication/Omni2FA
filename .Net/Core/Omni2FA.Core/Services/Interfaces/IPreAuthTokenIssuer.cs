namespace Omni2FA.Core.Services.Interfaces;

/// <summary>
/// Issues and validates the two short-lived tokens around a 2FA ceremony: the pre-auth token
/// (after password, for <c>/challenge/*</c>) and the verified-handoff token (after the challenge
/// passes, for the host's finalize step). They carry different purposes and are validated
/// separately, so neither can stand in for the other.
/// </summary>
public interface IPreAuthTokenIssuer {
    /// <summary>Mint a pre-auth token for the user, valid for the configured TTL.</summary>
    PreAuthTokenInfo Issue(string userId);

    /// <summary>Validate a pre-auth token. Returns the user id, or null if invalid, expired, or not a pre-auth token.</summary>
    string? ValidateAndGetUserId(string token);

    /// <summary>Mint a verified-handoff token after a challenge passes, valid for <see cref="Configuration.PreAuthOptions.VerifiedTtl"/>.</summary>
    PreAuthTokenInfo IssueVerified(string userId);

    /// <summary>Validate a verified-handoff token in finalize. Returns the trusted user id, or null if invalid, expired, or not a verified token.</summary>
    string? ValidateVerified(string token);
}
