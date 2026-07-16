using Flowist.NotificationService.IntegrationTests.TestSupport;
using Flowist.NotificationService.Services;

using FluentAssertions;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Flowist.NotificationService.IntegrationTests.SignalR;

public sealed class NotificationHubIntegrationTests : IClassFixture<NotificationServiceWebApplicationFactory>
{
    private readonly NotificationServiceWebApplicationFactory _factory;

    public NotificationHubIntegrationTests(NotificationServiceWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NotificationHub_ShouldStoreConnectionInRedisOnConnect()
    {
        Guid userId = Guid.NewGuid();
        using HttpClient client = _factory.CreateClient();

        HubConnection connection = new HubConnectionBuilder()
            .WithUrl(new Uri(client.BaseAddress!, "/hubs/notification"), options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, userId.ToString());
            })
            .Build();

        try
        {
            await connection.StartAsync();

            connection.ConnectionId.Should().NotBeNullOrWhiteSpace();

            using IServiceScope scope = _factory.Services.CreateScope();
            IUserConnectionManager connectionManager = scope.ServiceProvider.GetRequiredService<IUserConnectionManager>();

            IReadOnlyCollection<string> connections = await WaitForConnectionAsync(
                connectionManager,
                userId,
                connection.ConnectionId!);

            connections.Should().Contain(connection.ConnectionId);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static async Task<IReadOnlyCollection<string>> WaitForConnectionAsync(
        IUserConnectionManager connectionManager,
        Guid userId,
        string connectionId)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            IReadOnlyCollection<string> connections = await connectionManager.GetConnectionsAsync(userId);

            if (connections.Contains(connectionId))
            {
                return connections;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }

        return await connectionManager.GetConnectionsAsync(userId);
    }
}