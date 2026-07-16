using System.Globalization;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace Flowist.AuthService.IntegrationTests.TestSupport;

public sealed class AuthServiceWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string RabbitMqUsername = "guest";
    private const string RabbitMqPassword = "guest";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("flowist_auth_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:3-management")
        .WithUsername(RabbitMqUsername)
        .WithPassword(RabbitMqPassword)
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:8")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _rabbitMq.StartAsync();
        await _redis.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        Dispose();
        await _redis.DisposeAsync();
        await _rabbitMq.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            Dictionary<string, string?> configuration = new()
            {
                ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:Redis"] = _redis.GetConnectionString(),
                ["RabbitMq:Host"] = _rabbitMq.Hostname,
                ["RabbitMq:Port"] = _rabbitMq.GetMappedPublicPort(5672).ToString(CultureInfo.InvariantCulture),
                ["RabbitMq:Username"] = RabbitMqUsername,
                ["RabbitMq:Password"] = RabbitMqPassword,
                ["Jwt:Issuer"] = "Flowist.AuthService.Tests",
                ["Jwt:Audience"] = "Flowist.IntegrationTests",
                ["Jwt:SecretKey"] = "INTEGRATION_TEST_SECRET_KEY_AT_LEAST_32_CHARS",
                ["Jwt:AccessTokenExpirationMinutes"] = "60",
                ["Jwt:RefreshTokenExpirationDays"] = "7"
            };

            configurationBuilder.AddInMemoryCollection(configuration);
        });
    }
}