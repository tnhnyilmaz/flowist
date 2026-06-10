namespace Flowist.Shared.Events;

public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredOn { get; }
    Guid CorrelationId { get; }
}
