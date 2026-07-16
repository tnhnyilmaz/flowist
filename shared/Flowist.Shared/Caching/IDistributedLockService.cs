namespace Flowist.Shared.Caching;

public interface IDistributedLockService
{
    Task<IDistributedLock?> TryAcquireAsync(
        string key,
        TimeSpan expiration,
        CancellationToken cancellationToken = default);
}

public interface IDistributedLock : IAsyncDisposable
{
    string Key { get; }
}