namespace Flowist.ActivityService.Entities;

public sealed class ProcessedEvent
{
    public Guid EventId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public DateTimeOffset ProcessedAt { get; set; }
}