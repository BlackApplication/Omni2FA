namespace Omni2FA.Core.Configuration;

/// <summary>
/// One population of users that authenticates separately from the others — staff and customers in the
/// same application, each with their own login, their own identity table, and their own session. Every
/// audience gets its own mount of the Omni2FA endpoints and its own subject namespace, so ids that
/// overlap between the two tables (staff user 42 and customer 42) can never resolve to the same 2FA
/// methods. Hosts with a single population never touch this — the default audience is implicit.
/// </summary>
public class Omni2FaAudienceOptions {
    /// <summary>Name of the implicit audience every host has. Used when no audience is named.</summary>
    public const string DefaultName = "default";

    /// <summary>
    /// Identifier used to reference this audience from <c>MapOmni2Fa</c>, <c>[Omni2FaAudience]</c>, and
    /// <see cref="Services.Interfaces.IOmni2FaAudienceRegistry"/>. Must be unique.
    /// </summary>
    public string Name { get; set; } = DefaultName;

    /// <summary>
    /// Mount point for this audience's endpoints. Defaults to <see cref="AspNetCoreOptions.RoutePrefix"/>
    /// for the default audience; every other audience must set its own, and no two may collide. Put it
    /// under the path segment that already identifies the audience to the host (e.g. <c>/api/portal/2fa</c>
    /// for a customer portal served from <c>/api/portal</c>) so the host's existing routing rules — cookie
    /// selection, CORS, an auth scheme chosen by path — apply to the 2FA endpoints too, without the host
    /// having to flag the requests some other way.
    /// </summary>
    public string RoutePrefix { get; set; } = string.Empty;

    /// <summary>
    /// Namespace prepended to this audience's user ids before anything is stored or looked up
    /// (e.g. <c>customer:</c> → subject <c>customer:42</c>). Null or empty leaves ids untouched, which is
    /// what the default audience does — existing rows keep their subjects. Must be unique across audiences.
    /// </summary>
    public string? SubjectPrefix { get; set; }

    /// <summary>
    /// Authentication scheme(s) applied to this audience's session-authenticated endpoints
    /// (<c>/methods</c>, <c>/enroll/*</c>, <c>/stepup/*</c>, <c>/recovery-codes/*</c>) — comma-separated,
    /// matching <c>AuthorizeAttribute.AuthenticationSchemes</c>. Null uses the host's default scheme.
    /// The login <c>/challenge/*</c> endpoints are never covered: they authenticate with the pre-auth
    /// token, before any session exists.
    /// </summary>
    public string? AuthenticationSchemes { get; set; }

    /// <summary>
    /// Authorization policy applied to this audience's session-authenticated endpoints. Null requires
    /// only an authenticated user (the host's default policy).
    /// </summary>
    public string? AuthorizationPolicy { get; set; }
}
