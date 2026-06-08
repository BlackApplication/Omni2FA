using System.Text;
using System.Text.Json;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Omni2FA.Core.Services.Interfaces;
using Omni2FA.Core.Services.WebAuthn;

namespace Omni2FA.WebAuthn;

/// <summary>Fido2NetLib-backed implementation of <see cref="IWebAuthnCeremonyService"/>. Holds no state — the configured <see cref="IFido2"/> carries relying-party settings.</summary>
public class Fido2WebAuthnCeremonyService : IWebAuthnCeremonyService {
    private readonly IFido2 _fido2;

    public Fido2WebAuthnCeremonyService(IFido2 fido2) {
        _fido2 = fido2;
    }

    public WebAuthnAttestationOptions CreateAttestationOptions(WebAuthnUserInfo user, IReadOnlyList<byte[]> excludeCredentialIds) {
        var fidoUser = new Fido2User {
            Id = Encoding.UTF8.GetBytes(user.UserId),
            Name = user.Name,
            DisplayName = user.DisplayName,
        };
        var exclude = excludeCredentialIds.Select(id => new PublicKeyCredentialDescriptor(id)).ToList();

        var options = _fido2.RequestNewCredential(new RequestNewCredentialParams {
            User = fidoUser,
            ExcludeCredentials = exclude,
            AuthenticatorSelection = new AuthenticatorSelection {
                ResidentKey = ResidentKeyRequirement.Required,
                UserVerification = UserVerificationRequirement.Preferred,
            },
            AttestationPreference = AttestationConveyancePreference.None,
        });
        return new WebAuthnAttestationOptions { OptionsJson = options.ToJson() };
    }

    public async Task<WebAuthnRegisteredCredential?> VerifyAttestationAsync(string optionsJson, string attestationResponseJson, Func<byte[], CancellationToken, Task<bool>> isCredentialIdUnique, CancellationToken cancellationToken = default) {
        try {
            var options = CredentialCreateOptions.FromJson(optionsJson);
            var response = JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(attestationResponseJson);
            if (response is null) {
                return null;
            }

            var made = await _fido2.MakeNewCredentialAsync(new MakeNewCredentialParams {
                AttestationResponse = response,
                OriginalOptions = options,
                IsCredentialIdUniqueToUserCallback = (args, ct) => isCredentialIdUnique(args.CredentialId, ct),
            }, cancellationToken).ConfigureAwait(false);

            return new WebAuthnRegisteredCredential {
                CredentialId = made.Id,
                PublicKey = made.PublicKey,
                SignCount = made.SignCount,
                Aaguid = made.AaGuid == Guid.Empty ? null : made.AaGuid,
            };
        } catch (Fido2VerificationException) {
            return null;
        }
    }

    public WebAuthnAssertionOptions CreateAssertionOptions(IReadOnlyList<byte[]> allowCredentialIds) {
        var allow = allowCredentialIds.Select(id => new PublicKeyCredentialDescriptor(id)).ToList();
        var options = _fido2.GetAssertionOptions(new GetAssertionOptionsParams {
            AllowedCredentials = allow,
            UserVerification = UserVerificationRequirement.Preferred,
        });
        return new WebAuthnAssertionOptions { OptionsJson = options.ToJson() };
    }

    public async Task<WebAuthnAssertionVerification?> VerifyAssertionAsync(string optionsJson, string assertionResponseJson, WebAuthnStoredCredential storedCredential, CancellationToken cancellationToken = default) {
        try {
            var options = AssertionOptions.FromJson(optionsJson);
            var response = JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(assertionResponseJson);
            if (response is null) {
                return null;
            }

            var result = await _fido2.MakeAssertionAsync(new MakeAssertionParams {
                AssertionResponse = response,
                OriginalOptions = options,
                StoredPublicKey = storedCredential.PublicKey,
                StoredSignatureCounter = storedCredential.SignCount,
                IsUserHandleOwnerOfCredentialIdCallback = (_, _) => Task.FromResult(true),
            }, cancellationToken).ConfigureAwait(false);

            return new WebAuthnAssertionVerification { SignCount = result.SignCount };
        } catch (Fido2VerificationException) {
            return null;
        }
    }
}
