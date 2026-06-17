using Flowist.Shared.Enums;

namespace Flowist.TaskService.DTOs;

public sealed record TaskFilterRequest(
    Flowist.Shared.Enums.TaskStatus? Status,
    TaskPriority? Priority,
    Guid? AssigneeId,
    DateTimeOffset? DueFrom,
    DateTimeOffset? DueTo,
    string? SortBy,
    bool SortDescending,
    int Page = 1,
    int PageSize = 20);