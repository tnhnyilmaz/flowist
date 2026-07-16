using Flowist.Shared.Caching;

namespace Flowist.AuthService.Services;

public sealed class RedisTokenBlacklistService : ITokenBlacklistService
{
    private const string KeyPrefix = "auth:blacklist:jwt:";

    private readonly ICacheService _cacheService;

    public RedisTokenBlacklistService(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task BlacklistAsync(
        string tokenId,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        TimeSpan ttl = expiresAt - DateTimeOffset.UtcNow;

        if (ttl <= TimeSpan.Zero)
        {
            return;
        }

        await _cacheService.SetAsync(
            GetKey(tokenId),
            true,
            ttl,
            cancellationToken);
    }

    public async Task<bool> IsBlacklistedAsync(
        string tokenId,
        CancellationToken cancellationToken = default)
    {
        return await _cacheService.ExistsAsync(GetKey(tokenId), cancellationToken);
    }

    private static string GetKey(string tokenId)
    {
        return CacheKeys.JwtBlacklist(tokenId);
    }
}