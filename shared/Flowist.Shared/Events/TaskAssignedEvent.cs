namespace Flowist.Shared.Events;

public sealed record TaskAssignedEvent(
    Guid TaskId,
    Guid AssignedTo,
    Guid AssignedBy,
    Guid WorkspaceId,
    DateTimeOffset AssignedAt,
    Guid CorrelationId) : IntegrationEvent(CorrelationId);