namespace Flowist.NotificationService.Services;

public interface IUserConnectionManager
{
    void AddConnection(Guid userId, string connectionId);

    void RemoveConnection(Guid userId, string connectionId);

    IReadOnlyCollection<string> GetConnections(Guid userId);
}