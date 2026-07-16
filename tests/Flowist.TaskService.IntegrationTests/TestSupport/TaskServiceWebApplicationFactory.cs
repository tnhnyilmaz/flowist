using System.Globalization;

using Flowist.TaskService.IntegrationTests.TestSupport;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace Flowist.TaskService.IntegrationTests.TestSupport;

public sealed class TaskServiceWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string RabbitMqUsername = "guest";
    private const string RabbitMqPassword = "guest";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("flowist_task_tests")
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


        builder.ConfigureTestServices(services =>
        {
            services
                .AddAuthentication(TestAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.SchemeName,
                    _ => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
            });
        });
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
                ["Jwt:Issuer"] = JwtTestTokenFactory.Issuer,
                ["Jwt:Audience"] = JwtTestTokenFactory.Audience,
                ["Jwt:SecretKey"] = JwtTestTokenFactory.SecretKey
            };

            configurationBuilder.AddInMemoryCollection(configuration);
        });
    }
}