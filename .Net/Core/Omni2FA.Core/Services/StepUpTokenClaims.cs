namespace Omni2FA.Core.Services;

/// <summary>
/// Claims recovered from a validated step-up token. <see cref="Jti"/> is the unique token id used to
/// enforce single use via <see cref="Stores.IStepUpNonceStore"/>; <see cref="ExpiresAt"/> bounds how
/// long the consumed-id record must be retained.
/// </summary>
public sealed record StepUpTokenClaims(string UserId, string Jti, DateTime ExpiresAt);
