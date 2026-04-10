using IIoT.Services.Common.Contracts.DapperQueries;
using IIoT.Services.Common.Events;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Result;
using AutoMapper;
using MassTransit;

namespace IIoT.ProductionService.Commands.Capacities;

/// <summary>
/// 业务指令:接收半小时产能上报。
/// 上位机推送到 HttpApi → 校验设备存在且活跃 → 发布到 MQ → DataWorker 消费落库。
/// 上位机身份已在轮询认证接口换取 DeviceId,后续数据流转全部以 DeviceId 为唯一标识。
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
    IDeviceIdentityQueryService deviceIdentityQuery,
    IMapper mapper,
    IPublishEndpoint publishEndpoint
) : ICommandHandler<ReceiveHourlyCapacityCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        ReceiveHourlyCapacityCommand request,
        CancellationToken cancellationToken)
    {
        if (request.DeviceId == Guid.Empty)
            return Result.Failure("数据接收失败:DeviceId 不能为空");

        var exists = await deviceIdentityQuery.ExistsAsync(
            request.DeviceId, cancellationToken);
        if (!exists)
            return Result.Failure("数据接收失败:设备不存在");

        var @event = mapper.Map<HourlyCapacityReceivedEvent>(request);
        await publishEndpoint.Publish(@event, cancellationToken);

        return Result.Success(true);
    }
}