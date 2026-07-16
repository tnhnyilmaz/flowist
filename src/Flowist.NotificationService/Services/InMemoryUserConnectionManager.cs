using System.Collections.Concurrent;

namespace Flowist.NotificationService.Services;

public sealed class InMemoryUserConnectionManager : IUserConnectionManager
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _connections = new();

    public Task AddConnectionAsync(
        Guid userId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        ConcurrentDictionary<string, byte> userConnections = _connections.GetOrAdd(
            userId,
            _ => new ConcurrentDictionary<string, byte>());

        userConnections.TryAdd(connectionId, 0);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<string>> GetConnectionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (!_connections.TryGetValue(userId, out ConcurrentDictionary<string, byte>? userConnections))
        {
            return Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
        }

        return Task.FromResult<IReadOnlyCollection<string>>(userConnections.Keys.ToArray());
    }

    public Task RemoveConnectionAsync(
        Guid userId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        if (!_connections.TryGetValue(userId, out ConcurrentDictionary<string, byte>? userConnections))
        {
            return Task.CompletedTask;
        }

        userConnections.TryRemove(connectionId, out _);

        if (userConnections.IsEmpty)
        {
            _connections.TryRemove(userId, out _);
        }

        return Task.CompletedTask;
    }
}