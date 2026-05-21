using Microsoft.EntityFrameworkCore;
using Omni2FA.Core.Entities;
using Omni2FA.Core.Enums;
using Omni2FA.Core.Stores;

namespace Omni2FA.AspNetCore.EntityFrameworkCore.Stores;

/// <summary>
/// EF Core implementation of <see cref="ITwoFactorMethodStore"/>. Works against any
/// <see cref="DbContext"/> that has called <c>modelBuilder.ApplyOmni2FaConfiguration()</c>
/// during <c>OnModelCreating</c>.
/// </summary>
public class TwoFactorMethodStore : ITwoFactorMethodStore {
    private readonly DbContext _context;

    public TwoFactorMethodStore(DbContext context) {
        _context = context;
    }

    private DbSet<TwoFactorMethod> Set => _context.Set<TwoFactorMethod>();

    public async Task<IReadOnlyList<TwoFactorMethod>> ListActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default) {
        var list = await Set
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.IsActive)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return list;
    }

    public Task<bool> HasActiveMethodsAsync(Guid userId, CancellationToken cancellationToken = default) {
        return Set
            .AsNoTracking()
            .AnyAsync(m => m.UserId == userId && m.IsActive, cancellationToken);
    }

    public Task<TwoFactorMethod?> GetActiveAsync(Guid methodId, Guid userId, CancellationToken cancellationToken = default) {
        return Set.FirstOrDefaultAsync(m => m.Id == methodId && m.UserId == userId && m.IsActive, cancellationToken);
    }

    public Task<TwoFactorMethod?> GetByTypeAsync(Guid userId, TwoFactorMethodType type, bool activeOnly = true, CancellationToken cancellationToken = default) {
        var query = Set.Where(m => m.UserId == userId && m.Type == type);
        if (activeOnly) {
            query = query.Where(m => m.IsActive);
        }
        return query.OrderByDescending(m => m.CreatedAt).FirstOrDefaultAsync(cancellationToken);
    }

    public Task AddAsync(TwoFactorMethod method, CancellationToken cancellationToken = default) {
        if (method.CreatedAt == default) {
            method.CreatedAt = DateTime.UtcNow;
        }
        Set.Add(method);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(TwoFactorMethod method, CancellationToken cancellationToken = default) {
        Set.Remove(method);
        return Task.CompletedTask;
    }

    public Task MarkUsedAsync(TwoFactorMethod method, CancellationToken cancellationToken = default) {
        method.LastUsedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
