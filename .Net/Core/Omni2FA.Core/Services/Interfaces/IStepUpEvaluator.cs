using Omni2FA.Core.Enums;

namespace Omni2FA.Core.Services.Interfaces;

/// <summary>
/// Decides whether a step-up-protected action may proceed. Holds all the security logic in one place
/// (token validity, identity binding, single use, enrollment check) so the MVC action filter and the
/// minimal-API endpoint filter are thin wrappers over it.
/// </summary>
public interface IStepUpEvaluator {
    /// <summary>
    /// Evaluate the current request for <paramref name="userId"/> given the presented step-up
    /// <paramref name="token"/> (null when the header was absent). On <see cref="StepUpVerdict.Satisfied"/>
    /// the token has been consumed and cannot be reused.
    /// </summary>
    Task<StepUpVerdict> EvaluateAsync(string userId, string? token, CancellationToken cancellationToken = default);
}
