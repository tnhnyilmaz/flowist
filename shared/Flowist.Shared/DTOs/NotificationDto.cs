using Flowist.Shared.Enums;

namespace Flowist.Shared.DTOs;

public sealed record NotificationDto(
    Guid Id,
    Guid UserId,
    NotificationType Type,
    string Message,
    bool IsRead,
    DateTimeOffset CreatedAt);