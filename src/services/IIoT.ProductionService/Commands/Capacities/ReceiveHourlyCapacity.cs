using IIoT.Services.Common.Contracts;
using IIoT.Services.Common.Events;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Result;
using AutoMapper;
using MassTransit;

namespace IIoT.ProductionService.Commands.Capacities;

/// <summary>
/// 业务指令：接收半小时产能汇总
/// 边缘端补传任务按时段上报
/// PlcName：区分同一上位机下多台 PLC 的产能来源（可空）
/// </summary>
public record ReceiveHourlyCapacityCommand(
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
) : ICommand<Result<bool>>;

public class ReceiveHourlyCapacityHandler(
    IDataQueryService dataQueryService,
    IMapper mapper,
    IPublishEndpoint publishEndpoint
) : ICommandHandler<ReceiveHourlyCapacityCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ReceiveHourlyCapacityCommand request, CancellationToken cancellationToken)
    {
        var deviceExists = await dataQueryService.AnyAsync(
            dataQueryService.Devices.Where(d => d.Id == request.DeviceId && d.IsActive));

        if (!deviceExists)
            return Result.Failure("数据接收失败：设备不存在或已停用");

        var @event = mapper.Map<HourlyCapacityReceivedEvent>(request);
        await publishEndpoint.Publish(@event, cancellationToken);

        return Result.Success(true);
    }
}