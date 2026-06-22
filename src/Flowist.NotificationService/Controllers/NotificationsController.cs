using System.Security.Claims;

using Flowist.NotificationService.Data;
using Flowist.NotificationService.DTOs;
using Flowist.NotificationService.Services;
using Flowist.Shared.DTOs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowist.NotificationService.Controllers;


[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class NotificationsController : ControllerBase
{

    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }


    /// <summary>
    /// Gets paged notifications for the authenticated user.
    /// </summary>
    /// <param name="request">The notification pagination request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The paged notifications.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResult<NotificationDto>>> GetNotifications(
        [FromQuery] NotificationQueryRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!TryGetCurrentUserId(out Guid currentUserId)) return Unauthorized();

        PagedResult<NotificationDto> notifications = await _notificationService.GetNotificationsAsync(
            currentUserId, request, cancellationToken
        );

        return Ok(notifications);
    }


    /// <summary>
    /// Gets the unread notification count for the authenticated user.
    /// </summary>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The unread notification count.</returns>

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out Guid currentUserId)) return Unauthorized();

        int unreadCount = await _notificationService.GetUnreadCountAsync(
            currentUserId, cancellationToken
        );

        return Ok(unreadCount);
    }



    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    /// <param name="id">The notification id.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>No content when the notification is marked as read.</returns>
    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out Guid currentUserId)) return Unauthorized();

        await _notificationService.MarkAsReadAsync(
            id,
            currentUserId,
            cancellationToken
        );
        return NoContent();
    }



    /// <summary>
    /// Marks all notifications as read for the authenticated user.
    /// </summary>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>No content when all notifications are marked as read.</returns>
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out Guid currentUserId)) return Unauthorized();

        await _notificationService.MarkAllAsReadAsync(
            currentUserId,
            cancellationToken
        );

        return NoContent();
    }



    /// <summary>
    /// Deletes a notification.
    /// </summary>
    /// <param name="id">The notification id.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>No content when the notification is deleted.</returns>

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out Guid currentUserId)) return Unauthorized();

        await _notificationService.DeleteAsync(
            id, currentUserId, cancellationToken
        );

        return NoContent();
    }






    private bool TryGetCurrentUserId(out Guid userId)
    {
        string? userIdClaim = User.FindFirstValue(Flowist.Shared.Constants.ClaimTypes.UserId);
        return Guid.TryParse(userIdClaim, out userId);
    }


    
}