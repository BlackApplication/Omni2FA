using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Omni2FA.AspNetCore.EntityFrameworkCore.Stores;
using Omni2FA.Core.Stores;

namespace Omni2FA.AspNetCore.EntityFrameworkCore.Extensions;

/// <summary>DI extension methods for registering EF Core implementations of Omni2FA store interfaces.</summary>
public static class ServiceCollectionExtensions {
    /// <summary>
    /// Register EF Core implementations of <see cref="ITwoFactorMethodStore"/> and
    /// <see cref="ITwoFactorChallengeStore"/> backed by the host's <typeparamref name="TDbContext"/>.
    /// Call this after <c>services.AddDbContext&lt;TDbContext&gt;(...)</c>.
    /// </summary>
    /// <typeparam name="TDbContext">The host's <see cref="DbContext"/> type, which must have called <c>ApplyOmni2FaConfiguration()</c> in <c>OnModelCreating</c>.</typeparam>
    public static IServiceCollection AddOmni2FaEntityFrameworkStore<TDbContext>(this IServiceCollection services) where TDbContext : DbContext {
        services.AddScoped<ITwoFactorMethodStore>(sp => new TwoFactorMethodStore(sp.GetRequiredService<TDbContext>()));
        services.AddScoped<ITwoFactorChallengeStore>(sp => new TwoFactorChallengeStore(sp.GetRequiredService<TDbContext>()));
        services.AddScoped<IRecoveryCodeStore>(sp => new RecoveryCodeStore(sp.GetRequiredService<TDbContext>()));
        return services;
    }
}
