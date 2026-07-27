using Fido2NetLib;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Omni2FA.AspNetCore.Audit;
using Omni2FA.AspNetCore.Email;
using Omni2FA.AspNetCore.Filters;
using Omni2FA.AspNetCore.Internal;
using Omni2FA.AspNetCore.Services;
using Omni2FA.AspNetCore.Services.Interfaces;
using Omni2FA.Core.Configuration;
using Omni2FA.Core.Services;
using Omni2FA.Core.Services.Interfaces;
using Omni2FA.Core.Stores;
using Omni2FA.AspNetCore.WebAuthn;

namespace Omni2FA.AspNetCore.Extensions;

/// <summary>DI registration entry point for the Omni2FA ASP.NET Core adapter.</summary>
public static class ServiceCollectionExtensions {
    /// <summary>
    /// Register Omni2FA core services, the ASP.NET Core adapter, and bind <see cref="Omni2FaOptions"/>
    /// from the supplied configuration callback. Storage adapters (e.g. <c>AddOmni2FaEntityFrameworkStore</c>)
    /// must be registered separately.
    /// </summary>
    public static IServiceCollection AddOmni2Fa(this IServiceCollection services, Action<Omni2FaOptions>? configure = null) {
        var options = services.AddOptions<Omni2FaOptions>();
        if (configure is not null) {
            options.Configure(configure);
        }
        // Fail fast at startup on the misconfiguration that would otherwise blow up mid-login.
        options
            .Validate(o => o.PreAuth.SigningKey.Length >= 32, "Omni2Fa:PreAuth:SigningKey must be at least 32 characters (HMAC-SHA256 signing key).")
            .Validate(HasUniqueAudienceNames, "Omni2Fa:AspNetCore:Audiences must have unique Name values.")
            .Validate(HasUniqueAudienceRoutePrefixes, "Each Omni2Fa audience needs its own RoutePrefix — two mounts on the same path collide.")
            .Validate(HasUniqueAudienceSubjectPrefixes, "Each Omni2Fa audience needs its own SubjectPrefix — sharing one merges two populations' 2FA methods.")
            .Validate(NonDefaultAudiencesHaveRoutePrefix, "Every Omni2Fa audience other than 'default' must set RoutePrefix (the default one inherits Omni2Fa:AspNetCore:RoutePrefix).")
            .ValidateOnStart();

        services.AddHttpContextAccessor();

        services.AddSingleton<IOmni2FaAudienceRegistry, Omni2FaAudienceRegistry>();

        services.AddSingleton<ITotpService, TotpService>();
        services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();
        services.AddSingleton<IPreAuthTokenIssuer, JwtPreAuthTokenIssuer>();

        // Step-up (action confirmation): single-use token replay guard defaults to in-memory; the
        // evaluator is scoped because it reaches the host-registered (scoped) method store.
        services.AddMemoryCache();
        services.TryAddSingleton<IStepUpNonceStore, InMemoryStepUpNonceStore>();
        services.AddScoped<IStepUpEvaluator, StepUpEvaluator>();

        services.AddScoped<ITwoFactorMethodService, TwoFactorMethodService>();
        services.AddScoped<ITotpEnrollmentService, TotpEnrollmentService>();
        services.AddScoped<ITwoFactorChallengeService, TwoFactorChallengeService>();
        services.AddScoped<IEmailEnrollmentService, EmailEnrollmentService>();
        services.AddScoped<IRecoveryCodeService, RecoveryCodeService>();
        services.AddScoped<IEnrollmentFinalizer, EnrollmentFinalizer>();

        // Audit is opt-in: default writes structured ILogger records; a host sink replaces it.
        services.TryAddSingleton<IOmni2FaAuditSink, LoggerAuditSink>();
        // The shared rate-limit window lives in this singleton; the filter resolves it per request.
        services.AddSingleton<Omni2FaRateLimiter>();
        services.AddSingleton<RateLimitFilter>();

        // Background maintenance: prune consumed/expired challenge rows.
        services.AddHostedService<ChallengePurgeBackgroundService>();

        // Email OTP: transport and copy are pluggable (TryAdd → host registrations win); the OTP
        // primitive is scoped so it can consume a host-registered scoped IEmailSender if present.
        services.TryAddSingleton<IEmailMessageBuilder, DefaultEmailMessageBuilder>();
        services.TryAddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IEmailOtpService, EmailOtpService>();

        // Delivery seam: EmailDispatcher honors EmailOptions.BackgroundDelivery — enqueue onto the
        // channel (drained by the hosted worker) or send inline. Always registered; the option, not
        // the registration, picks the path, so it stays togglable from configuration at runtime.
        services.AddSingleton<EmailDispatchChannel>();
        services.AddScoped<IEmailDispatcher, EmailDispatcher>();
        services.AddHostedService<EmailDispatchBackgroundService>();

        // WebAuthn: a single Fido2 instance carries the relying-party config, built lazily so the
        // bound WebAuthnOptions are available. Ceremony impl is replaceable via TryAdd.
        services.TryAddSingleton<IFido2>(sp => {
            var webAuthn = sp.GetRequiredService<IOptions<Omni2FaOptions>>().Value.WebAuthn;
            return new Fido2(new Fido2Configuration {
                ServerDomain = webAuthn.RelyingPartyId,
                ServerName = webAuthn.RelyingPartyName,
                Origins = webAuthn.Origins.ToHashSet(StringComparer.Ordinal),
            });
        });
        services.TryAddSingleton<IWebAuthnCeremonyService, Fido2WebAuthnCeremonyService>();
        services.AddScoped<IWebAuthnEnrollmentService, WebAuthnEnrollmentService>();

        services.TryAddSingleton<IUserContextAccessor, UserContextAccessor>();

        services.AddSingleton<PreAuthFilter>();

        return services;
    }

    private static bool HasUniqueAudienceNames(Omni2FaOptions options) {
        var names = options.AspNetCore.Audiences.Select(a => a.Name).ToList();
        return names.Distinct(StringComparer.OrdinalIgnoreCase).Count() == names.Count;
    }

    private static bool HasUniqueAudienceRoutePrefixes(Omni2FaOptions options) {
        var prefixes = options.AspNetCore.Audiences
            .Select(a => string.IsNullOrWhiteSpace(a.RoutePrefix) ? options.AspNetCore.RoutePrefix : a.RoutePrefix)
            .ToList();
        // The implicit default mount counts too, unless an entry named 'default' already represents it.
        if (!options.AspNetCore.Audiences.Any(a => string.Equals(a.Name, Omni2FaAudienceOptions.DefaultName, StringComparison.OrdinalIgnoreCase))) {
            prefixes.Add(options.AspNetCore.RoutePrefix);
        }
        return prefixes.Distinct(StringComparer.OrdinalIgnoreCase).Count() == prefixes.Count;
    }

    private static bool HasUniqueAudienceSubjectPrefixes(Omni2FaOptions options) {
        var prefixes = options.AspNetCore.Audiences
            .Select(a => a.SubjectPrefix)
            .Where(prefix => !string.IsNullOrEmpty(prefix))
            .ToList();
        return prefixes.Distinct(StringComparer.Ordinal).Count() == prefixes.Count;
    }

    private static bool NonDefaultAudiencesHaveRoutePrefix(Omni2FaOptions options) {
        return options.AspNetCore.Audiences.All(a =>
            string.Equals(a.Name, Omni2FaAudienceOptions.DefaultName, StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrWhiteSpace(a.RoutePrefix));
    }
}
