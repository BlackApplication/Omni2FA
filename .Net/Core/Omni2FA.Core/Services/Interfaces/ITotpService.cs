namespace Omni2FA.Core.Services.Interfaces;

/// <summary>
/// Pure TOTP operations — generate fresh secrets, build authenticator-app URIs, validate codes.
/// No persistence, no encryption (see <see cref="ISecretProtector"/> for that), no I/O.
/// </summary>
public interface ITotpService {
    /// <summary>Generate a fresh random base32-encoded secret of length configured by <c>TotpOptions.SecretByteLength</c>.</summary>
    string GenerateSecret();

    /// <summary>
    /// Build the <c>otpauth://</c> URI that an authenticator app encodes into a QR code.
    /// </summary>
    /// <param name="accountLabel">Account label shown in the app, typically the user's email or username.</param>
    /// <param name="base32Secret">The unprotected base32 secret from <see cref="GenerateSecret"/>.</param>
    string BuildOtpAuthUri(string accountLabel, string base32Secret);

    /// <summary>
    /// Validate a user-submitted code against a base32 secret. Returns true if the code matches
    /// the current TOTP step (or within the configured tolerance window of steps).
    /// </summary>
    bool ValidateCode(string base32Secret, string code);
}
