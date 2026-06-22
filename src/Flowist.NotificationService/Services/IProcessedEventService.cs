namespace Flowist.NotificationService.Services;

public interface IProcessedEventService
{
    Task<bool> IsProcessedAsync(Guid eventId, CancellationToken cancellationToken = default);

    void MarkAsProcessed(Guid eventId, string eventType);
}