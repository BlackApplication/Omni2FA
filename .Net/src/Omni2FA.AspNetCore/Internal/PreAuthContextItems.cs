namespace Omni2FA.AspNetCore.Internal;

/// <summary>Keys placed on <c>HttpContext.Items</c> by the pre-auth filter.</summary>
internal static class PreAuthContextItems {
    /// <summary>UserId decoded from a validated pre-auth token. Read by <c>/challenge/*</c> endpoints.</summary>
    public const string UserId = "Omni2Fa:PreAuthUserId";
}
