namespace Omni2FA.AspNetCore.Routing;

/// <summary>
/// Endpoint metadata naming the audience a request belongs to. Read by the default
/// <c>IUserContextAccessor</c> (to namespace the subject) and by the step-up gate (to point the caller at
/// that audience's <c>/stepup</c> mount). Absent metadata means the default audience.
/// </summary>
public interface IOmni2FaAudienceMetadata {
    /// <summary>Name of the audience, matching an entry in <c>AspNetCoreOptions.Audiences</c>.</summary>
    string AudienceName { get; }
}
