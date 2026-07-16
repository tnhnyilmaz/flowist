namespace Flowist.NotificationService.Services;

public interface IUserConnectionManager
{
    Task AddConnectionAsync(
        Guid userId,
        string connectionId,
        CancellationToken cancellationToken = default);

    Task RemoveConnectionAsync(
        Guid userId,
        string connectionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> GetConnectionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}