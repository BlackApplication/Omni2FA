using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Omni2FA.AspNetCore.EntityFrameworkCore.Options;
using Omni2FA.Core.Entities;

namespace Omni2FA.AspNetCore.EntityFrameworkCore.Configurations;

/// <summary>EF Core mapping for <see cref="RecoveryCode"/>.</summary>
public class RecoveryCodeConfiguration : IEntityTypeConfiguration<RecoveryCode> {
    private readonly EfMappingOptions _options;

    public RecoveryCodeConfiguration(EfMappingOptions options) {
        _options = options;
    }

    public void Configure(EntityTypeBuilder<RecoveryCode> builder) {
        if (string.IsNullOrEmpty(_options.Schema)) {
            builder.ToTable(_options.RecoveryCodesTableName);
        } else {
            builder.ToTable(_options.RecoveryCodesTableName, _options.Schema);
        }

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId).HasMaxLength(64).IsRequired();
        builder.Property(c => c.CodeHash).HasMaxLength(128).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasIndex(c => c.UserId)
            .HasDatabaseName($"IX_{_options.RecoveryCodesTableName}_UserId");
    }
}
