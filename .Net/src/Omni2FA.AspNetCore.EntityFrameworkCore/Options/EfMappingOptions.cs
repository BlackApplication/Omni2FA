namespace Omni2FA.AspNetCore.EntityFrameworkCore.Options;

/// <summary>
/// Mapping options passed to <c>modelBuilder.ApplyOmni2FaConfiguration(...)</c>. Lets hosts
/// migrating from a custom 2FA implementation keep their existing table and column names so
/// no data renaming is required.
/// </summary>
public class EfMappingOptions {
    /// <summary>
    /// Database schema to place Omni2FA tables in. Empty string = provider default
    /// (<c>public</c> for PostgreSQL, <c>dbo</c> for SQL Server, none for SQLite).
    /// </summary>
    public string Schema { get; set; } = string.Empty;

    /// <summary>Table name for enrolled 2FA methods.</summary>
    public string MethodsTableName { get; set; } = "Omni2FaMethods";

    /// <summary>Table name for in-progress 2FA challenges.</summary>
    public string ChallengesTableName { get; set; } = "Omni2FaChallenges";

    /// <summary>Table name for one-time recovery codes.</summary>
    public string RecoveryCodesTableName { get; set; } = "Omni2FaRecoveryCodes";
}
