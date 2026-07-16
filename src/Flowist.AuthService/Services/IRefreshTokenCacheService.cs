namespace Flowist.AuthService.Services;

public interface IRefreshTokenCacheService
{
    Task CacheAsync(
        string refreshToken,
        Guid userId,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);

    Task<Guid?> GetUserIdAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}