using IIoT.Services.Common.Contracts;
using StackExchange.Redis;

namespace IIoT.Infrastructure.Locking;

/// <summary>
/// 基于 Redis SET NX EX + Lua 原子解锁 的分布式锁实现。
/// 使用 Aspire 托管的 IConnectionMultiplexer，不额外创建 Redis 连接。
/// </summary>
public class RedisDistributedLockService(IConnectionMultiplexer redis) : IDistributedLockService
{
    // 锁的自动过期时间 = 60 秒（防止进程崩溃后锁永久残留）
    private static readonly TimeSpan LockTtl = TimeSpan.FromSeconds(60);

    // Lua 脚本：原子性地校验 value 并删除，防止误删他人的锁
    private const string UnlockScript =
        "if redis.call('get', KEYS[1]) == ARGV[1] then " +
        "  return redis.call('del', KEYS[1]) " +
        "else " +
        "  return 0 " +
        "end";

    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<IAsyncDisposable> AcquireAsync(
        string resource,
        TimeSpan? acquireTimeout = null,
        CancellationToken cancellationToken = default)
    {
        var lockValue = Guid.NewGuid().ToString("N");
        var deadline = DateTime.UtcNow.Add(acquireTimeout ?? TimeSpan.FromSeconds(10));

        // 轮询式抢锁：每 100ms 重试一次，直到超时
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var acquired = await _db.StringSetAsync(
                resource, lockValue, LockTtl, When.NotExists);

            if (acquired)
                return new RedisLockHandle(_db, resource, lockValue);

            await Task.Delay(100, cancellationToken);
        }

        throw new TimeoutException($"获取分布式锁超时（{acquireTimeout?.TotalSeconds ?? 10}s）: {resource}");
    }

    /// <summary>锁句柄 — await using 释放时执行 Lua 原子解锁</summary>
    private sealed class RedisLockHandle(IDatabase db, string key, string value) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await db.ScriptEvaluateAsync(
                UnlockScript,
                keys: [(RedisKey)key],
                values: [(RedisValue)value]);
        }
    }
}
