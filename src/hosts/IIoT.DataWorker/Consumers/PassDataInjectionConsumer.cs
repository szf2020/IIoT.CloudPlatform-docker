using IIoT.ProductionService.Commands.PassStations;
using IIoT.Services.Common.Events;
using MassTransit;
using MediatR;

namespace IIoT.DataWorker.Consumers;

/// <summary>
/// 注液工序过站数据消费者。
/// MQ 接收 PassDataInjectionReceivedEvent(批量结构)→ 派发
/// PersistInjectionPassCommand → Handler 调 Repository InsertBatch 落库。
/// 幂等性由 SQL 层 ON CONFLICT DO NOTHING 保证。
/// 并发消费 4 线程,过站数据吞吐最高。
/// </summary>
public sealed class PassDataInjectionConsumer(ISender sender)
    : IConsumer<PassDataInjectionReceivedEvent>
{
    public async Task Consume(ConsumeContext<PassDataInjectionReceivedEvent> context)
    {
        var command = new PersistInjectionPassCommand(context.Message);
        var result = await sender.Send(command, context.CancellationToken);

        if (!result.IsSuccess)
            throw new InvalidOperationException(
                $"注液过站数据批量落库失败:{string.Join("; ", result.Errors)}");
    }
}
