namespace Flowist.TaskService.DTOs;

public sealed record UpdateProjectRequest(
    string Name,
    string? Description);