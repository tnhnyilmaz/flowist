using Flowist.AuthService.Data;

using Microsoft.EntityFrameworkCore;

namespace Flowist.AuthService.Services;

public sealed class ExpiredRefreshTokenCleanupService : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredRefreshTokenCleanupService> _logger;

    public ExpiredRefreshTokenCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpiredRefreshTokenCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(CleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupExpiredTokensAsync(stoppingToken);

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task CleanupExpiredTokensAsync(CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            AuthDbContext dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            DateTimeOffset now = DateTimeOffset.UtcNow;

            int deletedCount = await dbContext.RefreshTokens
                .Where(refreshToken => refreshToken.ExpiresAt < now)
                .ExecuteDeleteAsync(cancellationToken);

            if (deletedCount > 0)
            {
                _logger.LogInformation("Deleted {DeletedCount} expired refresh tokens.", deletedCount);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to cleanup expired refresh tokens.");
        }
    }
}