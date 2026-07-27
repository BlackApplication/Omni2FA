using Omni2FA.Core.Configuration;

namespace Omni2FA.AspNetCore.Internal;

internal static class EndpointNaming {
    /// <summary>
    /// Endpoint name for an operation on this audience's mount. Endpoint names are unique application-wide,
    /// so every mount past the default suffixes its own — otherwise a second <c>MapOmni2Fa</c> would fail at
    /// startup with a duplicate-name error. The default audience keeps the bare name, which is the
    /// operation id in the OpenAPI contract.
    /// </summary>
    public static string For(Omni2FaAudienceOptions audience, string operation) {
        return string.Equals(audience.Name, Omni2FaAudienceOptions.DefaultName, StringComparison.OrdinalIgnoreCase)
            ? operation
            : $"{operation}-{audience.Name}";
    }
}
