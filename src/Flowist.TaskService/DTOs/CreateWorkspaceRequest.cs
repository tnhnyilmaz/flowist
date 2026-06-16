namespace Flowist.TaskService.DTOs;

public sealed record CreateWorkspaceRequest(
    string Name,
    string? Description);