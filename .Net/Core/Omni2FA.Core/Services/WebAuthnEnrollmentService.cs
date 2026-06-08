using System.Text;
using Microsoft.Extensions.Options;
using Omni2FA.Core.Configuration;
using Omni2FA.Core.Dtos;
using Omni2FA.Core.Entities;
using Omni2FA.Core.Enums;
using Omni2FA.Core.Errors;
using Omni2FA.Core.Results;
using Omni2FA.Core.Services.Interfaces;
using Omni2FA.Core.Services.WebAuthn;
using Omni2FA.Core.Stores;

namespace Omni2FA.Core.Services;

/// <summary>Default WebAuthn enrollment ceremony — start (issues creation options) and confirm (verifies attestation).</summary>
public class WebAuthnEnrollmentService : IWebAuthnEnrollmentService {
    private readonly ITwoFactorMethodStore _methods;
    private readonly ITwoFactorChallengeStore _challenges;
    private readonly IWebAuthnCeremonyService _ceremony;
    private readonly int _maxCredentials;
    private readonly TimeSpan _enrollmentTtl;

    public WebAuthnEnrollmentService(
        ITwoFactorMethodStore methods,
        ITwoFactorChallengeStore challenges,
        IWebAuthnCeremonyService ceremony,
        IOptions<Omni2FaOptions> options) {
        _methods = methods;
        _challenges = challenges;
        _ceremony = ceremony;
        _maxCredentials = options.Value.WebAuthn.MaxCredentialsPerUser;
        _enrollmentTtl = options.Value.AspNetCore.EnrollmentTtl;
    }

    public async Task<Result<WebAuthnEnrollStartResponse>> StartAsync(string userId, string accountLabel, CancellationToken cancellationToken = default) {
        var existing = await GetWebAuthnMethodsAsync(userId, cancellationToken).ConfigureAwait(false);
        if (existing.Count >= _maxCredentials) {
            return Result<WebAuthnEnrollStartResponse>.Failure(Omni2FaErrorCodes.MaxMethodsReached);
        }

        var excludeCredentialIds = existing
            .Where(m => m.WebAuthnCredentialId is not null)
            .Select(m => m.WebAuthnCredentialId!)
            .ToList();

        var user = new WebAuthnUserInfo { UserId = userId, Name = accountLabel, DisplayName = accountLabel };
        var options = _ceremony.CreateAttestationOptions(user, excludeCredentialIds);

        var now = DateTime.UtcNow;
        var challenge = new TwoFactorChallenge {
            Id = Guid.NewGuid(),
            UserId = userId,
            Kind = TwoFactorChallengeKind.EnrollWebAuthn,
            WebAuthnChallenge = Encoding.UTF8.GetBytes(options.OptionsJson),
            CreatedAt = now,
            ExpiresAt = now.Add(_enrollmentTtl),
        };
        await _challenges.AddAsync(challenge, cancellationToken).ConfigureAwait(false);
        await _challenges.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<WebAuthnEnrollStartResponse>.Success(new WebAuthnEnrollStartResponse {
            EnrollmentId = challenge.Id,
            OptionsJson = options.OptionsJson,
        });
    }

    public async Task<Result<MethodCreatedResponse>> ConfirmAsync(string userId, WebAuthnEnrollConfirmRequest request, CancellationToken cancellationToken = default) {
        var challenge = await _challenges.GetActiveAsync(request.EnrollmentId, userId, cancellationToken).ConfigureAwait(false);
        if (challenge is null || challenge.Kind != TwoFactorChallengeKind.EnrollWebAuthn || challenge.WebAuthnChallenge is null) {
            return Result<MethodCreatedResponse>.Failure(Omni2FaErrorCodes.ChallengeNotFound);
        }

        var optionsJson = Encoding.UTF8.GetString(challenge.WebAuthnChallenge);
        var credential = await _ceremony.VerifyAttestationAsync(
            optionsJson,
            request.AttestationResponseJson,
            async (credentialId, ct) => !await _methods.WebAuthnCredentialExistsAsync(credentialId, ct).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);
        if (credential is null) {
            await _challenges.IncrementFailedAttemptsAsync(challenge, cancellationToken).ConfigureAwait(false);
            await _challenges.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<MethodCreatedResponse>.Failure(Omni2FaErrorCodes.WebAuthnVerificationFailed);
        }

        var method = new TwoFactorMethod {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = TwoFactorMethodType.WebAuthn,
            Name = request.Name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            WebAuthnCredentialId = credential.CredentialId,
            WebAuthnPublicKey = credential.PublicKey,
            WebAuthnSignCount = credential.SignCount,
            WebAuthnAaguid = credential.Aaguid,
        };
        await _methods.AddAsync(method, cancellationToken).ConfigureAwait(false);
        await _challenges.MarkConsumedAsync(challenge, cancellationToken).ConfigureAwait(false);
        await _methods.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<MethodCreatedResponse>.Success(new MethodCreatedResponse {
            MethodId = method.Id,
        });
    }

    private async Task<IReadOnlyList<TwoFactorMethod>> GetWebAuthnMethodsAsync(string userId, CancellationToken cancellationToken) {
        var all = await _methods.ListActiveByUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return all.Where(m => m.Type == TwoFactorMethodType.WebAuthn).ToList();
    }
}
