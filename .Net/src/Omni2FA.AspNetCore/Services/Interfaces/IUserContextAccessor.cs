namespace Omni2FA.AspNetCore.Services.Interfaces;

/// <summary>
/// Extracts the current user's identity from the ambient context for host-session endpoints
/// (<c>/methods/*</c>, <c>/enroll/*</c>). Default implementation reads configured claims from
/// <c>HttpContext.User</c>; hosts with non-standard identity sources override via DI replacement.
/// </summary>
public interface IUserContextAccessor {
    /// <summary>The current user's id. Throws if not authenticated.</summary>
    string GetCurrentUserId();

    /// <summary>Human-readable label shown in authenticator apps. Falls back to the user id if no label claim is present.</summary>
    string GetCurrentUserLabel();

    /// <summary>
    /// The current user's email address, used as the OTP destination for Email enrollment when
    /// <c>EmailEnrollmentAddressSource.ClaimOnly</c> (the default) is in effect. The default implementation
    /// reads the configured email claim; hosts whose authoritative address lives elsewhere (e.g. a User
    /// table) override just this method. Throws if no address can be resolved.
    /// </summary>
    string GetCurrentUserEmail();
}
