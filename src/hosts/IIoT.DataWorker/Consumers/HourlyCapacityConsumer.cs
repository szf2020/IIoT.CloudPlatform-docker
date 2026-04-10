using IIoT.ProductionService.Commands.Capacities;
using IIoT.Services.Common.Events;
using MassTransit;
using MediatR;

namespace IIoT.DataWorker.Consumers;

/// <summary>
/// 半小时产能数据消费者。
/// MQ 接收 HourlyCapacityReceivedEvent → 派发 PersistHourlyCapacityCommand →
/// PersistHourlyCapacityHandler 调 Repository Upsert 落库。
/// 单线程消费(ConcurrentMessageLimit = 1),保证同一设备同槽位的幂等顺序。
/// </summary>
public sealed class HourlyCapacityConsumer(ISender sender)
    : IConsumer<HourlyCapacityReceivedEvent>
{
    public async Task Consume(ConsumeContext<HourlyCapacityReceivedEvent> context)
    {
        var command = new PersistHourlyCapacityCommand(context.Message);
        var result = await sender.Send(command, context.CancellationToken);

        if (!result.IsSuccess)
            throw new InvalidOperationException(
                $"半小时产能落库失败:{string.Join("; ", result.Errors)}");
    }
}
