using Fido2NetLib;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Omni2FA.AspNetCore.Email;
using Omni2FA.AspNetCore.Filters;
using Omni2FA.AspNetCore.Services;
using Omni2FA.AspNetCore.Services.Interfaces;
using Omni2FA.Core.Configuration;
using Omni2FA.Core.Services;
using Omni2FA.Core.Services.Interfaces;
using Omni2FA.WebAuthn;

namespace Omni2FA.AspNetCore.Extensions;

/// <summary>DI registration entry point for the Omni2FA ASP.NET Core adapter.</summary>
public static class ServiceCollectionExtensions {
    /// <summary>
    /// Register Omni2FA core services, the ASP.NET Core adapter, and bind <see cref="Omni2FaOptions"/>
    /// from the supplied configuration callback. Storage adapters (e.g. <c>AddOmni2FaEntityFrameworkStore</c>)
    /// must be registered separately.
    /// </summary>
    public static IServiceCollection AddOmni2Fa(this IServiceCollection services, Action<Omni2FaOptions>? configure = null) {
        if (configure is not null) {
            services.Configure(configure);
        } else {
            services.AddOptions<Omni2FaOptions>();
        }

        services.AddHttpContextAccessor();

        services.AddSingleton<ITotpService, TotpService>();
        services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();
        services.AddSingleton<IPreAuthTokenIssuer, JwtPreAuthTokenIssuer>();

        services.AddScoped<ITwoFactorMethodService, TwoFactorMethodService>();
        services.AddScoped<ITotpEnrollmentService, TotpEnrollmentService>();
        services.AddScoped<ITwoFactorChallengeService, TwoFactorChallengeService>();
        services.AddScoped<IEmailEnrollmentService, EmailEnrollmentService>();

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
}
