namespace Flowist.Shared.Events;

public sealed record TaskCreatedEvent(
    Guid TaskId,
    string Title,
    Guid ProjectId,
    Guid WorkspaceId,
    Guid CreatedBy,
    DateTimeOffset CreatedAt,
    Guid CorrelationId) : IntegrationEvent(CorrelationId);