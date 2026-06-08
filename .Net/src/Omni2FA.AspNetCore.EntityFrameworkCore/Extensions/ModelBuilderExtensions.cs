using Microsoft.EntityFrameworkCore;
using Omni2FA.AspNetCore.EntityFrameworkCore.Configurations;
using Omni2FA.AspNetCore.EntityFrameworkCore.Options;

namespace Omni2FA.AspNetCore.EntityFrameworkCore.Extensions;

/// <summary>Extension methods for wiring Omni2FA's EF Core entity mappings into a host DbContext.</summary>
public static class ModelBuilderExtensions {
    /// <summary>
    /// Apply Omni2FA's entity configurations (TwoFactorMethod, TwoFactorChallenge) to the given
    /// <see cref="ModelBuilder"/>. Call this from your <c>DbContext.OnModelCreating</c>:
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder) {
    ///     base.OnModelCreating(modelBuilder);
    ///     modelBuilder.ApplyOmni2FaConfiguration();
    /// }
    /// </code>
    /// </summary>
    /// <param name="modelBuilder">The model builder from <c>OnModelCreating</c>.</param>
    /// <param name="configure">
    /// Optional callback to customize table/column names and schema — useful when migrating
    /// from an existing custom 2FA implementation that uses different table names.
    /// </param>
    public static ModelBuilder ApplyOmni2FaConfiguration(this ModelBuilder modelBuilder, Action<EfMappingOptions>? configure = null) {
        var options = new EfMappingOptions();
        configure?.Invoke(options);
        modelBuilder.ApplyConfiguration(new TwoFactorMethodConfiguration(options));
        modelBuilder.ApplyConfiguration(new TwoFactorChallengeConfiguration(options));
        modelBuilder.ApplyConfiguration(new RecoveryCodeConfiguration(options));
        return modelBuilder;
    }
}
