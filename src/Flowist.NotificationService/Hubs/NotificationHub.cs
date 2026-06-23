using System.Security.Claims;

using Flowist.NotificationService.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Flowist.NotificationService.Hubs;

[Authorize]
public sealed class NotificationHub : Hub
{

    private readonly IUserConnectionManager _userConnectionManager;
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(IUserConnectionManager userConnectionManager, ILogger<NotificationHub> logger)
    {
        _logger = logger;
        _userConnectionManager = userConnectionManager;
    }

    public override async Task OnConnectedAsync()
    {
        if (!TryGetCurrentUserId(out Guid userId))
        {
            Context.Abort();
            return;
        }

        _userConnectionManager.AddConnection(userId, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(userId));

        _logger.LogInformation(
            "User {UserId} connected to notification hub with connection {ConnectionId}",
            userId,
            Context.ConnectionId
        );

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (TryGetCurrentUserId(out Guid userId))
        {
            _userConnectionManager.RemoveConnection(userId, Context.ConnectionId);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetUserGroupName(userId));

            _logger.LogInformation(
                exception,
                "User {UserId} disconnect from notification hub with connection {ConnectionId}",
                userId,
                Context.ConnectionId
            );
        }
        await base.OnDisconnectedAsync(exception);
    }
    private bool TryGetCurrentUserId(out Guid userId)
    {
        string? userIdClaim = Context.User?.FindFirstValue(Flowist.Shared.Constants.ClaimTypes.UserId);

        return Guid.TryParse(userIdClaim, out userId);
    }

    public static string GetUserGroupName(Guid userId)
    {
        return $"user:{userId}";
    }

}