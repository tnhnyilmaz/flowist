using System.Net;
using System.Net.Http.Json;

using Flowist.NotificationService.Data;
using Flowist.NotificationService.Entities;
using Flowist.NotificationService.IntegrationTests.TestSupport;
using Flowist.Shared.DTOs;
using Flowist.Shared.Enums;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

namespace Flowist.NotificationService.IntegrationTests.Api;

public sealed class NotificationApiIntegrationTests : IClassFixture<NotificationServiceWebApplicationFactory>
{
    private readonly NotificationServiceWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public NotificationApiIntegrationTests(NotificationServiceWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task NotificationsApi_ShouldListUnreadCountAndMarkAsRead()
    {
        Guid userId = Guid.NewGuid();
        Guid otherUserId = Guid.NewGuid();
        Notification unreadNotification = await SeedNotificationAsync(userId, isRead: false);
        await SeedNotificationAsync(userId, isRead: true);
        await SeedNotificationAsync(otherUserId, isRead: false);
        UseUser(userId);

        PagedResult<NotificationDto>? notifications = await _client.GetFromJsonAsync<PagedResult<NotificationDto>>("/api/notifications");

        notifications.Should().NotBeNull();
        notifications!.TotalCount.Should().Be(2);
        notifications.Items.Should().OnlyContain(notification => notification.UserId == userId);

        int unreadCount = await _client.GetFromJsonAsync<int>("/api/notifications/unread-count");
        unreadCount.Should().Be(1);

        HttpResponseMessage markAsReadResponse = await _client.PutAsync($"/api/notifications/{unreadNotification.Id}/read", content: null);
        markAsReadResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        int updatedUnreadCount = await _client.GetFromJsonAsync<int>("/api/notifications/unread-count");
        updatedUnreadCount.Should().Be(0);
    }

    private async Task<Notification> SeedNotificationAsync(Guid userId, bool isRead)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        NotificationDbContext dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        Notification notification = new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = NotificationType.TaskAssigned,
            Title = "Integration notification",
            Message = "Notification message",
            IsRead = isRead,
            CreatedAt = DateTimeOffset.UtcNow,
            ReadAt = isRead ? DateTimeOffset.UtcNow : null
        };

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync();

        return notification;
    }

    private void UseUser(Guid userId)
    {
        _client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.UserIdHeaderName);
        _client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeaderName, userId.ToString());
    }
}