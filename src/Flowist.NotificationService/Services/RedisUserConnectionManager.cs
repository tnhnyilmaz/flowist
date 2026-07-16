using Flowist.Shared.Caching;

using StackExchange.Redis;

namespace Flowist.NotificationService.Services;

public sealed class RedisUserConnectionManager : IUserConnectionManager
{
    private const string KeyPrefix = "signalr:user-connections:";
    private static readonly TimeSpan ConnectionExpiration = CacheExpirationDefaults.SignalRConnectionState;

    private readonly IDatabase _database;

    public RedisUserConnectionManager(IConnectionMultiplexer connectionMultiplexer)
    {
        _database = connectionMultiplexer.GetDatabase();
    }

    public async Task AddConnectionAsync(
        Guid userId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        string key = GetKey(userId);

        await _database.SetAddAsync(key, connectionId);
        await _database.KeyExpireAsync(key, ConnectionExpiration);
    }

    public async Task<IReadOnlyCollection<string>> GetConnectionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        RedisValue[] connections = await _database.SetMembersAsync(GetKey(userId));

        return connections
            .Select(connection => connection.ToString())
            .Where(connection => !string.IsNullOrWhiteSpace(connection))
            .ToArray();
    }

    public async Task RemoveConnectionAsync(
        Guid userId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        string key = GetKey(userId);

        await _database.SetRemoveAsync(key, connectionId);

        long remainingConnections = await _database.SetLengthAsync(key);

        if (remainingConnections == 0)
        {
            await _database.KeyDeleteAsync(key);
        }
    }

    private static string GetKey(Guid userId)
    {
        return CacheKeys.SignalRUserConnections(userId);
    }
}