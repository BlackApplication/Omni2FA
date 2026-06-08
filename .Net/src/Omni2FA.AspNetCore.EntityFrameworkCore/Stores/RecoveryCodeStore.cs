using Microsoft.EntityFrameworkCore;
using Omni2FA.Core.Entities;
using Omni2FA.Core.Stores;

namespace Omni2FA.AspNetCore.EntityFrameworkCore.Stores;

/// <summary>
/// EF Core implementation of <see cref="IRecoveryCodeStore"/>. Works against any <see cref="DbContext"/>
/// that has called <c>modelBuilder.ApplyOmni2FaConfiguration()</c> during <c>OnModelCreating</c>.
/// </summary>
public class RecoveryCodeStore : IRecoveryCodeStore {
    private readonly DbContext _context;

    public RecoveryCodeStore(DbContext context) {
        _context = context;
    }

    private DbSet<RecoveryCode> Set => _context.Set<RecoveryCode>();

    public async Task<IReadOnlyList<RecoveryCode>> ListByUserAsync(string userId, CancellationToken cancellationToken = default) {
        return await Set.Where(c => c.UserId == userId).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> HasAnyAsync(string userId, CancellationToken cancellationToken = default) {
        return Set.AsNoTracking().AnyAsync(c => c.UserId == userId, cancellationToken);
    }

    public Task AddRangeAsync(IEnumerable<RecoveryCode> codes, CancellationToken cancellationToken = default) {
        Set.AddRange(codes);
        return Task.CompletedTask;
    }

    public Task MarkUsedAsync(RecoveryCode code, CancellationToken cancellationToken = default) {
        code.UsedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public Task<int> DeleteByUserAsync(string userId, CancellationToken cancellationToken = default) {
        return Set.Where(c => c.UserId == userId).ExecuteDeleteAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
