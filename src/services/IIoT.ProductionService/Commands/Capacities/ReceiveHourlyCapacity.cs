using IIoT.Services.Common.Events;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Result;
using AutoMapper;
using MassTransit;
using IIoT.Services.Common.Contracts;

namespace IIoT.ProductionService.Commands.Capacities;

/// <summary>
/// 业务指令：接收半小时槽位产能
/// 边缘端实时上报半小时聚合产能数据
/// </summary>
public record ReceiveHourlyCapacityCommand(
    Guid DeviceId,
    DateOnly Date,
    int Hour,
    int Minute,
    string TimeLabel,
    string ShiftCode,
    int TotalCount,
    int OkCount,
    int NgCount
) : ICommand<Result<bool>>;

public class ReceiveHourlyCapacityHandler(
    IDataQueryService dataQueryService,
    IMapper mapper,
    IPublishEndpoint publishEndpoint
) : ICommandHandler<ReceiveHourlyCapacityCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ReceiveHourlyCapacityCommand request, CancellationToken cancellationToken)
    {
        // 1. 校验设备是否存在且激活
        var deviceExists = await dataQueryService.AnyAsync(
            dataQueryService.Devices.Where(d => d.Id == request.DeviceId && d.IsActive)
        );

        if (!deviceExists)
            return Result.Failure("数据接收失败：设备不存在或已停用");

        // 2. 转换为事件并发布到 MQ
        var @event = mapper.Map<HourlyCapacityReceivedEvent>(request);
        await publishEndpoint.Publish(@event, cancellationToken);

        return Result.Success(true);
    }
}
