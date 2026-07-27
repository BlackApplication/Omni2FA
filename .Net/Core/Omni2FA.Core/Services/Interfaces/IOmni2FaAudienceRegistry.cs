using Omni2FA.Core.Configuration;

namespace Omni2FA.Core.Services.Interfaces;

/// <summary>
/// Resolves configured audiences and translates between a host's own user ids and the namespaced
/// subjects Omni2FA stores. The endpoints do this for you — inject this only where the host itself
/// talks to Omni2FA outside a mounted endpoint: issuing the pre-auth token at login, reading the
/// subject back out of a verified-handoff token at finalize, or resetting a user's 2FA.
/// </summary>
public interface IOmni2FaAudienceRegistry {
    /// <summary>
    /// The audience registered under <paramref name="name"/>; the default audience when null or empty.
    /// Throws <see cref="InvalidOperationException"/> for an unknown name — a typo here would silently
    /// read another population's methods, so it fails loudly instead.
    /// </summary>
    Omni2FaAudienceOptions Resolve(string? name = null);

    /// <summary>All configured audiences, the default one first.</summary>
    IReadOnlyList<Omni2FaAudienceOptions> All { get; }

    /// <summary>The stored subject for a user id within an audience — the id itself when the audience has no namespace.</summary>
    string ToSubject(string? audienceName, string userId);

    /// <summary>
    /// The host's own user id back out of a stored subject. False when the subject belongs to a different
    /// audience, which is the check to make before trusting a subject that arrived in a token.
    /// </summary>
    bool TryGetUserId(string? audienceName, string subject, out string userId);
}
