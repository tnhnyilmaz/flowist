using Flowist.Shared.Caching;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using StackExchange.Redis;

namespace Flowist.Shared.Extensions;

public static class RedisCacheServiceCollectionExtensions
{
    public static IServiceCollection AddRedisCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Redis")
            ?? configuration["Redis:ConnectionString"]
            ?? throw new InvalidOperationException("Redis connection string is missing.");

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            return ConnectionMultiplexer.Connect(connectionString);
        });

        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<IDistributedLockService, RedisDistributedLockService>();

        return services;
    }
}