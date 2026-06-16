namespace Flowist.TaskService.DTOs;

public sealed record UpdateWorkspaceRequest(
    string Name,
    string? Description);