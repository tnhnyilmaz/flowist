using System.Collections.Concurrent;

namespace Flowist.NotificationService.Services;

public sealed class InMemoryUserConnectionManager : IUserConnectionManager
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _connections = new();


    public void AddConnection(Guid userId, string connectionId)
    {
        ConcurrentDictionary<string, byte> userConnections = _connections.GetOrAdd(
            userId,
            _ => new ConcurrentDictionary<string, byte>()
        );
        userConnections.TryAdd(connectionId, 0);
    }

    public IReadOnlyCollection<string> GetConnections(Guid userId)
    {
        throw new NotImplementedException();
    }

    public void RemoveConnection(Guid userId, string connectionId)
    {
        if (!_connections.TryGetValue(userId, out ConcurrentDictionary<string, byte>? userConnections))
        {
            return;
        }

        userConnections.TryRemove(connectionId, out _);

        if (userConnections.IsEmpty) _connections.TryRemove(userId, out _);
    }
}