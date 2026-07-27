using Microsoft.Extensions.Options;
using Omni2FA.Core.Configuration;
using Omni2FA.Core.Services.Interfaces;

namespace Omni2FA.Core.Services;

/// <summary>
/// Default <see cref="IOmni2FaAudienceRegistry"/> over <see cref="AspNetCoreOptions.Audiences"/>. The
/// default audience is implicit: hosts that never configure an audience still resolve one, mounted at
/// <see cref="AspNetCoreOptions.RoutePrefix"/> with no subject namespace, so single-population hosts
/// behave exactly as they did before audiences existed.
/// </summary>
public class Omni2FaAudienceRegistry : IOmni2FaAudienceRegistry {
    private readonly Dictionary<string, Omni2FaAudienceOptions> _byName;
    private readonly List<Omni2FaAudienceOptions> _all;

    public Omni2FaAudienceRegistry(IOptions<Omni2FaOptions> options) {
        var aspNetCore = options.Value.AspNetCore;
        var configured = aspNetCore.Audiences;

        var @default = configured.FirstOrDefault(a => string.Equals(a.Name, Omni2FaAudienceOptions.DefaultName, StringComparison.OrdinalIgnoreCase))
            ?? new Omni2FaAudienceOptions { Name = Omni2FaAudienceOptions.DefaultName };
        if (string.IsNullOrWhiteSpace(@default.RoutePrefix)) {
            @default.RoutePrefix = aspNetCore.RoutePrefix;
        }

        _all = [@default, .. configured.Where(a => !ReferenceEquals(a, @default))];
        _byName = _all.ToDictionary(a => a.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<Omni2FaAudienceOptions> All => _all;

    public Omni2FaAudienceOptions Resolve(string? name = null) {
        if (string.IsNullOrWhiteSpace(name)) {
            return _all[0];
        }
        if (_byName.TryGetValue(name, out var audience)) {
            return audience;
        }
        throw new InvalidOperationException(
            $"No Omni2FA audience named '{name}'. Configured: {string.Join(", ", _byName.Keys)}. " +
            "Add it to Omni2FaOptions.AspNetCore.Audiences, or drop the name to use the default audience.");
    }

    public string ToSubject(string? audienceName, string userId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        var prefix = Resolve(audienceName).SubjectPrefix;
        return string.IsNullOrEmpty(prefix) ? userId : prefix + userId;
    }

    public bool TryGetUserId(string? audienceName, string subject, out string userId) {
        userId = string.Empty;
        if (string.IsNullOrEmpty(subject)) {
            return false;
        }

        var prefix = Resolve(audienceName).SubjectPrefix;
        if (string.IsNullOrEmpty(prefix)) {
            // The default namespace has no marker of its own, so a subject carrying any other audience's
            // prefix would otherwise pass as one of its own ids.
            if (_all.Any(a => a.SubjectPrefix is { Length: > 0 } other && subject.StartsWith(other, StringComparison.Ordinal))) {
                return false;
            }
            userId = subject;
            return true;
        }

        if (!subject.StartsWith(prefix, StringComparison.Ordinal) || subject.Length == prefix.Length) {
            return false;
        }
        userId = subject[prefix.Length..];
        return true;
    }
}
