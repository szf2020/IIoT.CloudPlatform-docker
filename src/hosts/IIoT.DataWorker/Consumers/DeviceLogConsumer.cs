using IIoT.Core.Production.Aggregates;
using IIoT.Core.Production.Aggregates.DeviceLogs;
using IIoT.EntityFrameworkCore;
using IIoT.Services.Common.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace IIoT.DataWorker.Consumers;

public class DeviceLogConsumer(
    IIoTDbContext dbContext,
    ILogger<DeviceLogConsumer> logger)
    : IConsumer<DeviceLogReceivedEvent>
{
    public async Task Consume(ConsumeContext<DeviceLogReceivedEvent> context)
    {
        var message = context.Message;
        logger.LogInformation("接收到设备日志批量推送，共 {Count} 条。", message.Logs.Count);

        if (message.Logs.Count == 0) return;

        var entities = message.Logs.Select(log =>
        {
            // 1. 无论服务器当前在哪个时区，强制认定上位机传来的时间是“北京时间(UTC+8)”
            var beijingOffset = TimeSpan.FromHours(8);
            var localOffsetTime = new DateTimeOffset(log.LogTime, beijingOffset);

            // 2. 转为绝对的 UTC 时间给 EF Core 存库
            var absoluteUtcTime = localOffsetTime.UtcDateTime;

            return new DeviceLog(
                log.DeviceId,
                log.Level,
                log.Message,
                absoluteUtcTime // 存入数据库，再也不会报错了
            );
        }).ToList();

        dbContext.DeviceLogs.AddRange(entities);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("设备日志批量写入成功，共 {Count} 条。", entities.Count);
    }
}