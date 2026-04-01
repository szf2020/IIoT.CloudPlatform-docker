using IIoT.DataWorker.Commands;
using IIoT.Services.Common.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IIoT.DataWorker.Consumers;

/// <summary>
/// 消费端：处理半小时槽位产能上报事件
/// 通过 MediatR Pipeline 转发到 UpsertHourlyCapacityCommand，经 DistributedLockBehavior 保护
/// </summary>
public class HourlyCapacityConsumer(
    ISender mediator,
    ILogger<HourlyCapacityConsumer> logger)
    : IConsumer<HourlyCapacityReceivedEvent>
{
    public async Task Consume(ConsumeContext<HourlyCapacityReceivedEvent> context)
    {
        var message = context.Message;
        logger.LogInformation(
            "接收到半小时槽位产能数据: DeviceId={DeviceId}, Date={Date}, Hour={Hour}, Minute={Minute}, ShiftCode={ShiftCode}",
            message.DeviceId, message.Date, message.Hour, message.Minute, message.ShiftCode);

        // 转发到 Upsert 命令，由 DistributedLockBehavior 保护并发
        var command = new UpsertHourlyCapacityCommand(
            message.DeviceId,
            message.Date,
            message.Hour,
            message.Minute,
            message.TimeLabel,
            message.ShiftCode,
            message.TotalCount,
            message.OkCount,
            message.NgCount);

        await mediator.Send(command, context.CancellationToken);

        logger.LogInformation("半小时槽位产能数据处理完成");
    }
}
