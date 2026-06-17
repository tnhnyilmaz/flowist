namespace Flowist.TaskService.DTOs;

public sealed record AssignTaskRequest(
    Guid AssigneeId);