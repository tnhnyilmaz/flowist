namespace Flowist.Shared.Events;

public sealed record UserRegisteredEvent(Guid UserId, string Email, string FullName, DateTimeOffset createdAt, Guid CorrelationId) : IntegrationEvent(CorrelationId);