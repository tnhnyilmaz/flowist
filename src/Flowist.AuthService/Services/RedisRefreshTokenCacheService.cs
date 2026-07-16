using System.Security.Cryptography;
using System.Text;

using Flowist.Shared.Caching;

namespace Flowist.AuthService.Services;

public sealed class RedisRefreshTokenCacheService : IRefreshTokenCacheService
{
    private const string KeyPrefix = "auth:refresh-token:";

    private readonly ICacheService _cacheService;

    public RedisRefreshTokenCacheService(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task CacheAsync(
        string refreshToken,
        Guid userId,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        TimeSpan ttl = expiresAt - DateTimeOffset.UtcNow;

        if (ttl <= TimeSpan.Zero)
        {
            return;
        }

        await _cacheService.SetAsync(
            GetKey(refreshToken),
            userId,
            ttl,
            cancellationToken);
    }

    public async Task<Guid?> GetUserIdAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        return await _cacheService.GetAsync<Guid?>(GetKey(refreshToken), cancellationToken);
    }

    public async Task RemoveAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        await _cacheService.DeleteAsync(GetKey(refreshToken), cancellationToken);
    }

    private static string GetKey(string refreshToken)
    {
        string tokenHash = ComputeSha256(refreshToken);

        return $"{KeyPrefix}{tokenHash}";
    }

    private static string ComputeSha256(string value)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));

        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}