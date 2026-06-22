namespace Flowist.NotificationService.DTOs;

public sealed record NotificationQueryRequest(
    int Page = 1,
    int PageSize = 20);