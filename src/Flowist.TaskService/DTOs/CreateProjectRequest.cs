namespace Flowist.TaskService.DTOs;

public sealed record CreateProjectRequest(
    string Name,
    string? Description);