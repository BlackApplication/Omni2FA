using Omni2FA.Core.Enums;
using Omni2FA.Core.Services.Interfaces;
using Omni2FA.Core.Stores;

namespace Omni2FA.Core.Services;

/// <summary>
/// Default <see cref="IStepUpEvaluator"/>. Stateless transport (a signed JWT in a header) plus a
/// single-use guard via <see cref="IStepUpNonceStore"/>. Security properties enforced here:
/// the token must be valid and unexpired, its subject must match the authenticated user (a stolen
/// token cannot be replayed against a different account), and it satisfies exactly one action — unless
/// the host opened a grace window (<see cref="Configuration.StepUpOptions.GraceWindow"/>), in which
/// case the token stays usable for the actions the user takes inside it.
/// </summary>
public class StepUpEvaluator : IStepUpEvaluator {
    private readonly IPreAuthTokenIssuer _tokens;
    private readonly IStepUpNonceStore _nonces;
    private readonly ITwoFactorMethodStore _methods;

    public StepUpEvaluator(IPreAuthTokenIssuer tokens, IStepUpNonceStore nonces, ITwoFactorMethodStore methods) {
        _tokens = tokens;
        _nonces = nonces;
        _methods = methods;
    }

    public async Task<StepUpVerdict> EvaluateAsync(string userId, string? token, CancellationToken cancellationToken = default) {
        if (!string.IsNullOrWhiteSpace(token)) {
            var claims = _tokens.ValidateStepUp(token);
            // Bind to the caller before spending the nonce: never burn a token that isn't this user's.
            if (claims is not null && string.Equals(claims.UserId, userId, StringComparison.Ordinal)) {
                // Leaving the nonce unspent keeps the token's single use for after the window closes.
                if (claims.GraceUntil is not null && DateTime.UtcNow < claims.GraceUntil.Value) {
                    return StepUpVerdict.Satisfied;
                }
                if (await _nonces.TryConsumeAsync(claims.Jti, claims.ExpiresAt, cancellationToken).ConfigureAwait(false)) {
                    return StepUpVerdict.Satisfied;
                }
            }
        }

        // No usable token. If the user has no 2FA there is nothing to step up to — let it through.
        var enrolled = await _methods.HasActiveMethodsAsync(userId, cancellationToken).ConfigureAwait(false);
        return enrolled ? StepUpVerdict.Required : StepUpVerdict.NotEnrolledBypass;
    }
}
