using IIoT.Core.Production.Aggregates.Capacities;
using IIoT.EntityFrameworkCore;
using IIoT.Services.Common.Attributes;
using IIoT.Services.Common.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IIoT.DataWorker.Commands;

/// <summary>
/// 产能 Upsert 指令（半小时槽位级） — 由 HourlyCapacityConsumer dispatch 到此，经 DistributedLockBehavior 保护。
/// 同一设备同一时段同一班次并发上报时，分布式锁确保串行执行，杜绝 INSERT 冲突。
/// </summary>
[DistributedLock("iiot:lock:capacity:hourly:{DeviceId}:{Date}:{Hour}:{Minute}:{ShiftCode}", TimeoutSeconds = 5)]
public record UpsertHourlyCapacityCommand(
    Guid DeviceId,
    DateOnly Date,
    int Hour,
    int Minute,
    string TimeLabel,
    string ShiftCode,
    int TotalCount,
    int OkCount,
    int NgCount
) : IRequest;

public class UpsertHourlyCapacityHandler(
    IIoTDbContext dbContext,
    ICacheService cacheService,
    ILogger<UpsertHourlyCapacityHandler> logger) : IRequestHandler<UpsertHourlyCapacityCommand>
{
    public async Task Handle(UpsertHourlyCapacityCommand request, CancellationToken cancellationToken)
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

        // 2. Upsert：同设备同时段同班次已存在则更新，否则新增
        var existing = await dbContext.HourlyCapacities
            .FirstOrDefaultAsync(c => c.DeviceId == request.DeviceId
                                   && c.Date == request.Date
                                   && c.Hour == request.Hour
                                   && c.Minute == request.Minute
                                   && c.ShiftCode == request.ShiftCode,
                cancellationToken);

        if (existing is not null)
        {
            existing.UpdateCapacity(request.TotalCount, request.OkCount, request.NgCount);
            logger.LogInformation("半小时槽位产能已存在，执行覆盖更新。");
        }
        else
        {
            var record = new HourlyCapacity(
                request.DeviceId, request.Date, request.Hour, request.Minute, request.TimeLabel,
                request.ShiftCode, request.TotalCount, request.OkCount, request.NgCount);
            dbContext.HourlyCapacities.Add(record);
            logger.LogInformation("新增半小时槽位产能记录。");
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // 3. 清除缓存
        var cacheKey = $"capacity:hourly:{request.DeviceId}:{request.Date}";
        await cacheService.RemoveAsync(cacheKey);
        logger.LogInformation("已清除产能缓存。");
    }
}
