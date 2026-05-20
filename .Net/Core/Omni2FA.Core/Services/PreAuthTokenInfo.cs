namespace Omni2FA.Core.Services;

/// <summary>Pair of an issued pre-auth token and its expiration timestamp (UTC).</summary>
public sealed record PreAuthTokenInfo(string Token, DateTime ExpiresAt);
