namespace Flowist.Shared.Events;

public sealed record ProjectCreatedEvent(
    Guid ProjectId,
    Guid WorkspaceId,
    string Name,
    Guid CreatedBy,
    DateTimeOffset CreatedAt,
    Guid CorrelationId) : IntegrationEvent(CorrelationId);