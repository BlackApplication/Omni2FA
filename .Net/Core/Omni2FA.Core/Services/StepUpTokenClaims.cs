namespace Omni2FA.Core.Services;

/// <summary>
/// Claims recovered from a validated step-up token. <see cref="Jti"/> is the unique token id used to
/// enforce single use via <see cref="Stores.IStepUpNonceStore"/>; <see cref="ExpiresAt"/> bounds how
/// long the consumed-id record must be retained. <see cref="GraceUntil"/> is set only when the host
/// configured <see cref="Configuration.StepUpOptions.GraceWindow"/>.
/// </summary>
/// <param name="UserId">Subject the token was issued to; must match the authenticated caller.</param>
/// <param name="Jti">Unique token id, spent once through <see cref="Stores.IStepUpNonceStore"/>.</param>
/// <param name="ExpiresAt">UTC expiry of the token itself.</param>
/// <param name="GraceUntil">
/// UTC instant until which the token satisfies protected calls repeatedly, without spending its single
/// use. <c>null</c> (the default) means the token is single-use from the moment it is issued.
/// </param>
public sealed record StepUpTokenClaims(string UserId, string Jti, DateTime ExpiresAt, DateTime? GraceUntil = null);
