namespace Flowist.Shared.DTOs;

public sealed record ProjectDto(
    Guid Id,
    string Name,
    string? Description,
    Guid WorkspaceId,
    DateTimeOffset CreatedAt);