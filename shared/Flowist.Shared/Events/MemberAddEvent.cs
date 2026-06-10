using Flowist.Shared.Enums;

namespace Flowist.Shared.Events;

public sealed record MemberAddedEvent(
    Guid WorkspaceId,
    Guid UserId,
    WorkspaceRole Role,
    Guid AddedBy,
    DateTimeOffset AddedAt,
    Guid CorrelationId) : IntegrationEvent(CorrelationId);