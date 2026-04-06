using IIoT.Core.Production.Aggregates.Capacities;
using IIoT.EntityFrameworkCore;
using IIoT.Services.Common.Attributes;
using IIoT.Services.Common.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IIoT.DataWorker.Commands;

[DistributedLock("iiot:lock:capacity-hourly:{DeviceId}:{Date}:{ShiftCode}:{Hour}:{Minute}", TimeoutSeconds = 5)]
public record UpsertHourlyCapacityCommand(
    Guid DeviceId,
    DateOnly Date,
    string ShiftCode,
    int Hour,
    int Minute,
    string TimeLabel,
    int TotalCount,
    int OkCount,
    int NgCount,
    string? PlcName = null
) : IRequest;

public class UpsertHourlyCapacityHandler(
    IIoTDbContext dbContext,
    ICacheService cacheService,
    ILogger<UpsertHourlyCapacityHandler> logger) : IRequestHandler<UpsertHourlyCapacityCommand>
{
    public async Task Handle(UpsertHourlyCapacityCommand request, CancellationToken cancellationToken)
    {
        var deviceExists = await dbContext.Devices
            .AsNoTracking()
            .AnyAsync(d => d.Id == request.DeviceId && d.IsActive, cancellationToken);

        if (!deviceExists)
        {
            logger.LogWarning("设备 {DeviceId} 不存在或已停用，跳过半小时产能写入。", request.DeviceId);
            return;
        }

        var existing = await dbContext.HourlyCapacities
            .FirstOrDefaultAsync(c => c.DeviceId == request.DeviceId
                                   && c.Date == request.Date
                                   && c.ShiftCode == request.ShiftCode
                                   && c.Hour == request.Hour
                                   && c.Minute == request.Minute,
                cancellationToken);

        if (existing is not null)
        {
            existing.TimeLabel = request.TimeLabel;
            existing.PlcName = request.PlcName;
            existing.UpdateCapacity(request.TotalCount, request.OkCount, request.NgCount);
            logger.LogInformation("半小时产能已存在，执行覆盖更新。");
        }
        else
        {
            dbContext.HourlyCapacities.Add(new HourlyCapacity(
                request.DeviceId,
                request.Date,
                request.ShiftCode,
                request.Hour,
                request.Minute,
                request.TimeLabel,
                request.TotalCount,
                request.OkCount,
                request.NgCount,
                request.PlcName));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await cacheService.RemoveByPatternAsync("iiot:capacity:paged:v1:*", cancellationToken);

        logger.LogInformation(
            "半小时产能写入成功: DeviceId={DeviceId}, PlcName={PlcName}, Date={Date}, Time={Hour}:{Minute}",
            request.DeviceId, request.PlcName ?? "(null)", request.Date, request.Hour, request.Minute);
    }
}