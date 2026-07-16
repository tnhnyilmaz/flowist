using Flowist.ActivityService.Data;
using Flowist.ActivityService.Entities;

using Microsoft.EntityFrameworkCore;

namespace Flowist.ActivityService.Services;

public sealed class ProcessedEventService : IProcessedEventService
{
    private readonly ActivityDbContext _dbContext;

    public ProcessedEventService(ActivityDbContext dbContext)
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