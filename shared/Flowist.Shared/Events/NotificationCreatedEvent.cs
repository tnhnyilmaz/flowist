namespace Flowist.Shared.Events;

public sealed record NotificationCreatedEvent(
    Guid NotificationId,
    Guid UserId,
    string Type,
    string Message,
    DateTimeOffset CreatedAt,
    Guid CorrelationId) : IntegrationEvent(CorrelationId);