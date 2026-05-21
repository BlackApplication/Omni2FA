using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Omni2FA.AspNetCore.Filters;
using Omni2FA.AspNetCore.Services;
using Omni2FA.AspNetCore.Services.Interfaces;
using Omni2FA.Core.Configuration;
using Omni2FA.Core.Services;
using Omni2FA.Core.Services.Interfaces;

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

        services.TryAddSingleton<IUserContextAccessor, UserContextAccessor>();

        services.AddSingleton<PreAuthFilter>();

        return services;
    }
}
