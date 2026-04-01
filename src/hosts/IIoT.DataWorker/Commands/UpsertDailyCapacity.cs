using IIoT.Core.Production.Aggregates.Capacities;
using IIoT.EntityFrameworkCore;
using IIoT.Services.Common.Attributes;
using IIoT.Services.Common.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IIoT.DataWorker.Commands;

/// <summary>
/// 产能 Upsert 指令 — 由 DailyCapacityConsumer dispatch 到此，经 DistributedLockBehavior 保护。
/// 同一设备同一班次并发上报时，分布式锁确保串行执行，杜绝 INSERT 冲突。
/// </summary>
[DistributedLock("iiot:lock:capacity:{DeviceId}:{Date}:{ShiftCode}", TimeoutSeconds = 5)]
public record UpsertDailyCapacityCommand(
    Guid DeviceId,
    DateOnly Date,
    string ShiftCode,
    int TotalCount,
    int OkCount,
    int NgCount
) : IRequest;

public class UpsertDailyCapacityHandler(
    IIoTDbContext dbContext,
    ICacheService cacheService,
    ILogger<UpsertDailyCapacityHandler> logger) : IRequestHandler<UpsertDailyCapacityCommand>
{
    public async Task Handle(UpsertDailyCapacityCommand request, CancellationToken cancellationToken)
    {
        // 1. 校验设备是否存在且激活
        var deviceExists = await dbContext.Devices
            .AsNoTracking()
            .AnyAsync(d => d.Id == request.DeviceId && d.IsActive, cancellationToken);

        if (!deviceExists)
        {
            logger.LogWarning("设备 {DeviceId} 不存在或已停用，跳过写入。", request.DeviceId);
            return;
        }

        // 2. Upsert：同设备同天同班次已存在则更新，否则新增
        var existing = await dbContext.DailyCapacities
            .FirstOrDefaultAsync(c => c.DeviceId == request.DeviceId
                                   && c.Date == request.Date
                                   && c.ShiftCode == request.ShiftCode,
                cancellationToken);

        if (existing is not null)
        {
            existing.UpdateCapacity(request.TotalCount, request.OkCount, request.NgCount);
            logger.LogInformation("产能汇总已存在，执行覆盖更新。");
        }
        else
        {
            var record = new DailyCapacity(
                request.DeviceId, request.Date, request.ShiftCode,
                request.TotalCount, request.OkCount, request.NgCount);
            dbContext.DailyCapacities.Add(record);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // 3. 缓存失效：按模式删除该设备的所有汇总缓存（含任意日期范围）+ 所有分页缓存
        await cacheService.RemoveByPatternAsync(
            $"iiot:capacity:summary:v1:{request.DeviceId}:*", cancellationToken);
        await cacheService.RemoveByPatternAsync(
            "iiot:capacity:paged:v1:*", cancellationToken);

        logger.LogInformation("产能汇总写入成功: DeviceId={DeviceId}, Date={Date}",
            request.DeviceId, request.Date);
    }
}
