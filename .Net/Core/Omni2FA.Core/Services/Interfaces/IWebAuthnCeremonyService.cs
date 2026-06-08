using Omni2FA.Core.Services.WebAuthn;

namespace Omni2FA.Core.Services.Interfaces;

/// <summary>
/// WebAuthn ceremony primitive — builds creation/assertion options and verifies browser responses.
/// Implemented by <c>Omni2FA.WebAuthn</c> on Fido2NetLib; the interface stays free of any FIDO2 types
/// so core orchestration never depends on the crypto library.
/// </summary>
public interface IWebAuthnCeremonyService {
    /// <summary>Build credential creation options, excluding the user's already-registered credentials.</summary>
    WebAuthnAttestationOptions CreateAttestationOptions(WebAuthnUserInfo user, IReadOnlyList<byte[]> excludeCredentialIds);

    /// <summary>
    /// Verify a browser attestation against the original options. Returns the new credential, or
    /// <c>null</c> if verification fails. <paramref name="isCredentialIdUnique"/> must report whether
    /// the produced credential id is not already registered to any user.
    /// </summary>
    Task<WebAuthnRegisteredCredential?> VerifyAttestationAsync(string optionsJson, string attestationResponseJson, Func<byte[], CancellationToken, Task<bool>> isCredentialIdUnique, CancellationToken cancellationToken = default);

    /// <summary>Build assertion (login) options allowing the given credential ids.</summary>
    WebAuthnAssertionOptions CreateAssertionOptions(IReadOnlyList<byte[]> allowCredentialIds);

    /// <summary>Verify a browser assertion against the original options and the stored credential. Returns the updated counter, or <c>null</c> on failure.</summary>
    Task<WebAuthnAssertionVerification?> VerifyAssertionAsync(string optionsJson, string assertionResponseJson, WebAuthnStoredCredential storedCredential, CancellationToken cancellationToken = default);
}
