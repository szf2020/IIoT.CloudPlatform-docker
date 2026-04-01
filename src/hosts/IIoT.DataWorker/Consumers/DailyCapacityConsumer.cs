using IIoT.DataWorker.Commands;
using IIoT.Services.Common.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IIoT.DataWorker.Consumers;

public class DailyCapacityConsumer(
    ISender sender,
    ILogger<DailyCapacityConsumer> logger)
    : IConsumer<DailyCapacityReceivedEvent>
{
    public async Task Consume(ConsumeContext<DailyCapacityReceivedEvent> context)
    {
        var message = context.Message;
        logger.LogInformation("接收到产能汇总: DeviceId={DeviceId}, Date={Date}, Shift={ShiftCode}",
            message.DeviceId, message.Date, message.ShiftCode);

        await sender.Send(new UpsertDailyCapacityCommand(
            message.DeviceId,
            message.Date,
            message.ShiftCode,
            message.TotalCount,
            message.OkCount,
            message.NgCount),
            context.CancellationToken);
    }
}