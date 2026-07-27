namespace Omni2FA.AspNetCore.Routing;

/// <summary>
/// Declares which audience a controller or action serves, so Omni2FA resolves the caller's subject in
/// that audience's namespace. Put it on the host's own endpoints for a non-default population — most
/// importantly the ones carrying <c>[RequireTwoFactor]</c>, which otherwise evaluate step-up against the
/// default audience's subject and would never match the caller's enrolled methods. Omni2FA's own mounted
/// endpoints are tagged by <c>MapOmni2Fa("name")</c> and need nothing here.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class Omni2FaAudienceAttribute(string audienceName) : Attribute, IOmni2FaAudienceMetadata {
    public string AudienceName { get; } = audienceName;
}
