using Flowist.NotificationService.Data;
using Flowist.NotificationService.Entities;

using MassTransit.SqlTransport;

using Microsoft.EntityFrameworkCore;

namespace Flowist.NotificationService.Services;

public sealed class ProcessedEventService : IProcessedEventService
{
    private readonly NotificationDbContext _dbContext;

    public ProcessedEventService(NotificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<bool> IsProcessedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProcessedEvents
            .AnyAsync(processedEvent => processedEvent.EventId == eventId, cancellationToken);
    }

    public void MarkAsProcessed(Guid eventId, string eventType)
    {
        _dbContext.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = eventId,
            EventType = eventType,
            ProcessedAt = DateTimeOffset.UtcNow
        });
    }
}