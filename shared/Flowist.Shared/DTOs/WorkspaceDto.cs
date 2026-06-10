namespace Flowist.Shared.DTOs;

public sealed record WorkspaceDto(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    DateTimeOffset CreatedAt);