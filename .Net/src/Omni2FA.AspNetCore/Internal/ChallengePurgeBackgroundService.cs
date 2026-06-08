using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Omni2FA.Core.Stores;

namespace Omni2FA.AspNetCore.Internal;

/// <summary>
/// Periodically deletes consumed/expired challenge rows so the table doesn't grow unbounded. Resolves
/// the store in a fresh scope each tick; a failure is logged and the loop continues.
/// </summary>
internal sealed partial class ChallengePurgeBackgroundService : BackgroundService {
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ChallengePurgeBackgroundService> _logger;

    public ChallengePurgeBackgroundService(IServiceScopeFactory scopeFactory, ILogger<ChallengePurgeBackgroundService> logger) {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        using var timer = new PeriodicTimer(Interval);
        do {
            try {
                using var scope = _scopeFactory.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<ITwoFactorChallengeStore>();
                var removed = await store.PurgeExpiredAsync(DateTime.UtcNow, stoppingToken).ConfigureAwait(false);
                if (removed > 0) {
                    LogPurged(removed);
                }
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                break;
            }
#pragma warning disable CA1031 // Purge is best-effort maintenance — never let one failure stop the loop.
            catch (Exception ex) {
                LogPurgeFailed(ex);
            }
#pragma warning restore CA1031
        } while (await WaitAsync(timer, stoppingToken).ConfigureAwait(false));
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken stoppingToken) {
        try {
            return await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Omni2FA purged {Count} expired 2FA challenge(s).")]
    private partial void LogPurged(int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Omni2FA failed to purge expired 2FA challenges.")]
    private partial void LogPurgeFailed(Exception exception);
}
