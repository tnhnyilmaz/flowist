namespace Flowist.Shared.Events;

public sealed record UserRegisteredEvent(Guid UserId, string Email, string FullName, Guid CorrelationId) : IntegrationEvent(CorrelationId);  