namespace Flowist.TaskService.DTOs;

public sealed record UpdateTaskStatusRequest(
    Flowist.Shared.Enums.TaskStatus Status);