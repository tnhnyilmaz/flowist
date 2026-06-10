namespace Flowist.Shared.DTOs;

public sealed record UserDto(
    Guid Id,
    string Email,
    string FullName,
    DateTimeOffset CreatedAt);