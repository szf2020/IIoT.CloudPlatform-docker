using IIoT.DataWorker.Commands;
using IIoT.Services.Common.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IIoT.DataWorker.Consumers;

public class HourlyCapacityConsumer(
    ISender sender,
    ILogger<HourlyCapacityConsumer> logger)
    : IConsumer<HourlyCapacityReceivedEvent>
{
    public async Task Consume(ConsumeContext<HourlyCapacityReceivedEvent> context)
    {
        var message = context.Message;
        logger.LogInformation(
            "接收到半小时产能: DeviceId={DeviceId}, PlcName={PlcName}, Date={Date}, Shift={ShiftCode}, Time={Hour}:{Minute}",
            message.DeviceId, message.PlcName ?? "(null)", message.Date, message.ShiftCode, message.Hour, message.Minute);

        await sender.Send(new UpsertHourlyCapacityCommand(
                message.DeviceId,
                message.Date,
                message.ShiftCode,
                message.Hour,
                message.Minute,
                message.TimeLabel,
                message.TotalCount,
                message.OkCount,
                message.NgCount,
                message.PlcName),
            context.CancellationToken);
    }
}