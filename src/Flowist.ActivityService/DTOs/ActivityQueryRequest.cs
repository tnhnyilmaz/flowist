using Flowist.Shared.Enums;

namespace Flowist.ActivityService.DTOs;

public sealed record ActivityQueryRequest(
    ActivityType? ActionType,
    Guid? UserId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? EntityType,
    int Page = 1,
    int PageSize = 20);