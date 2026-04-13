using IIoT.ProductionService.Commands.DeviceLogs;
using IIoT.Services.Common.Events;
using MassTransit;
using MediatR;

namespace IIoT.DataWorker.Consumers;

/// <summary>
/// 设备日志消费者。
/// MQ 接收 DeviceLogReceivedEvent(整批共享同一 DeviceId)→
/// 派发 PersistDeviceLogCommand → Handler 调 Repository InsertBatch 落库。
/// 并发消费 3 线程,多设备日志并行落库互不阻塞。
/// </summary>
public sealed class DeviceLogConsumer(ISender sender)
    : IConsumer<DeviceLogReceivedEvent>
{
    public async Task Consume(ConsumeContext<DeviceLogReceivedEvent> context)
    {
        var command = new PersistDeviceLogCommand(context.Message);
        var result = await sender.Send(command, context.CancellationToken);

        if (!result.IsSuccess)
            throw new InvalidOperationException(
                $"设备日志批量落库失败:{string.Join("; ", result.Errors ?? [])}");
    }
}
