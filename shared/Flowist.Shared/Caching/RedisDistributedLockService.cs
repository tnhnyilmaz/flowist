using StackExchange.Redis;

namespace Flowist.Shared.Caching;

public sealed class RedisDistributedLockService : IDistributedLockService
{
    private readonly IDatabase _database;

    public RedisDistributedLockService(IConnectionMultiplexer connectionMultiplexer)
    {
        _database = connectionMultiplexer.GetDatabase();
    }

    public async Task<IDistributedLock?> TryAcquireAsync(
        string key,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        string lockValue = Guid.NewGuid().ToString("N");

        bool acquired = await _database.StringSetAsync(
            key,
            lockValue,
            expiration,
            When.NotExists);

        if (!acquired)
        {
            return null;
        }

        return new RedisDistributedLock(_database, key, lockValue);
    }

    private sealed class RedisDistributedLock : IDistributedLock
    {
        private const string ReleaseScript = """
            if redis.call("GET", KEYS[1]) == ARGV[1] then
                return redis.call("DEL", KEYS[1])
            else
                return 0
            end
            """;

        private readonly IDatabase _database;
        private readonly string _lockValue;
        private bool _disposed;

        public RedisDistributedLock(
            IDatabase database,
            string key,
            string lockValue)
        {
            _database = database;
            Key = key;
            _lockValue = lockValue;
        }

        public string Key { get; }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            await _database.ScriptEvaluateAsync(
                ReleaseScript,
                [Key],
                [_lockValue]);
        }
    }
}