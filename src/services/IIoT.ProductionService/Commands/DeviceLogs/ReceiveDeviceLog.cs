using IIoT.Services.Common.Contracts.DapperQueries;
using IIoT.Services.Common.Events;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Result;
using MassTransit;

namespace IIoT.ProductionService.Commands.DeviceLogs;

/// <summary>
/// 业务指令:接收设备日志(批量上报)。
/// 边缘端 LogPushTask 定时批量推送 → 校验设备存在且活跃 → 发布到 MQ → DataWorker 消费落库。
/// 一个上位机进程对应一个 Device,一批日志共享同一 DeviceId,
/// 因此 DeviceId 在 Command 顶层只出现一次,Logs 集合内每条 Item 不再重复携带。
/// 上位机身份已在轮询认证接口换取 DeviceId,后续数据流转全部以 DeviceId 为唯一标识。
/// </summary>
public record ReceiveDeviceLogCommand(
    Guid DeviceId,
    List<DeviceLogItem> Logs
) : ICommand<Result<bool>>;

public class ReceiveDeviceLogHandler(
    IDeviceIdentityQueryService deviceIdentityQuery,
    IPublishEndpoint publishEndpoint
) : ICommandHandler<ReceiveDeviceLogCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        ReceiveDeviceLogCommand request,
        CancellationToken cancellationToken)
    {
        if (request.DeviceId == Guid.Empty)
            return Result.Failure("数据接收失败:DeviceId 不能为空");

        if (request.Logs is null || request.Logs.Count == 0)
            return Result.Failure("数据接收失败:日志列表不能为空");

        var exists = await deviceIdentityQuery.ExistsAsync(
            request.DeviceId, cancellationToken);
        if (!exists)
            return Result.Failure("数据接收失败:设备不存在");

        var @event = new DeviceLogReceivedEvent
        {
            DeviceId = request.DeviceId,
            Logs = request.Logs
        };

        await publishEndpoint.Publish(@event, cancellationToken);

        return Result.Success(true);
    }
}