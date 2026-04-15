using IIoT.Infrastructure.Authentication;
using IIoT.Infrastructure.Caching;
using IIoT.Infrastructure.Locking;
using IIoT.Services.Common.Contracts;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace IIoT.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructures(this IHostApplicationBuilder builder)
    {
        builder.AddRedisDistributedCache("redis-cache");

        builder.Services.AddFusionCache()
            .WithDefaultEntryOptions(new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromMinutes(5),
                IsFailSafeEnabled = true,
                FailSafeMaxDuration = TimeSpan.FromHours(24),
                FailSafeThrottleDuration = TimeSpan.FromSeconds(10)
            })
            .WithSystemTextJsonSerializer()
            .WithDistributedCache(provider => provider.GetRequiredService<IDistributedCache>())
            .WithStackExchangeRedisBackplane(_ => { });

        builder.Services.AddOptions<RedisBackplaneOptions>()
            .Configure<IConnectionMultiplexer>((opt, cm) =>
                opt.ConnectionMultiplexerFactory = async () => cm);

        builder.Services.Configure<JwtSettings>(
            builder.Configuration.GetSection(JwtSettings.SectionName));
        builder.Services.PostConfigure<JwtSettings>(options =>
        {
            options.Secret = JwtSecretResolver.Resolve(builder.Environment, options.Secret);
        });

        builder.Services.AddSingleton<ICacheService, RedisCacheService>();
        builder.Services.AddSingleton<IDistributedLockService, RedisDistributedLockService>();
        builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
    }
}
