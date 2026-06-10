using Flowist.Shared.Enums;

namespace Flowist.Shared.DTOs;

public sealed record ActivityLogDto(
    Guid Id,
    Guid WorkspaceId,
    Guid UserId,
    ActivityType ActionType,
    string Description,
    DateTimeOffset CreatedAt);