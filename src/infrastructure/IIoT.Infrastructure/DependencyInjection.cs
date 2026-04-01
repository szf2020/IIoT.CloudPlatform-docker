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

namespace IIoT.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructures(this IHostApplicationBuilder builder)
    {
        builder.AddRedisDistributedCache("redis-cache");
        
        // 配置 FusionCache（L1 + L2 两级缓存 + Backplane 多实例同步）
        builder.Services.AddFusionCache()
            .WithDefaultEntryOptions(new FusionCacheEntryOptions
            {
                // L1（内存缓存）配置：5 分钟 + 10% 随机抖动（防止缓存同时过期导致雪崩）
                Duration = TimeSpan.FromMinutes(5),
                IsFailSafeEnabled = true,  // 启用失败转移：Redis 挂了返回过期缓存
                FailSafeMaxDuration = TimeSpan.FromHours(24),  // 失败转移最长持续 24 小时
                FailSafeThrottleDuration = TimeSpan.FromSeconds(10),  // 防击穿：并发请求自动合并 10 秒
            })
            .WithDistributedCache(provider => provider.GetRequiredService<IDistributedCache>())
            .WithStackExchangeRedisBackplane(opt => { }); // 连接由下方 Options 注入

        // Backplane 使用 Aspire 托管的 IConnectionMultiplexer，避免重复创建 Redis 连接
        builder.Services.AddOptions<RedisBackplaneOptions>()
            .Configure<IConnectionMultiplexer>((opt, cm) =>
                opt.ConnectionMultiplexerFactory = async () => cm);

        // 绑定配置
        builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
        
        // 注册 ICacheService 实现（使用 FusionCache）
        builder.Services.AddSingleton<ICacheService, RedisCacheService>();

        // 注册分布式锁服务
        builder.Services.AddSingleton<IDistributedLockService, RedisDistributedLockService>();

        // 注册 JWT 生成器 (单例即可，因为它只是个纯函数计算器)
        builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
    }
}