namespace Flowist.Shared.Events;

public abstract record IntegrationEvent : IIntegrationEvent
{
    protected IntegrationEvent(Guid correlationId)
    {
        EventId = Guid.NewGuid();
        OccurredOn = DateTimeOffset.UtcNow;
        CorrelationId = correlationId;
    }

    protected IntegrationEvent(Guid eventId, DateTimeOffset occurredOn, Guid correlationId)
    {
        EventId = eventId;
        OccurredOn = occurredOn;
        CorrelationId = correlationId;
    }

    public Guid EventId { get; init; }

    public DateTimeOffset OccurredOn { get; init; }

    public Guid CorrelationId { get; init; }
}
