using IIoT.Services.Common.Contracts.DapperQueries;
using IIoT.Services.Common.Events;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Result;
using MassTransit;

namespace IIoT.ProductionService.Commands.PassStations;

/// <summary>
/// 业务指令:接收注液工序过站数据(批量)。
/// 上位机推送到 HttpApi → 校验设备存在 → 整批时间戳 UTC 归一化 →
/// 发布到 MQ → DataWorker 消费后批量落库。
/// 一个上位机进程对应一个 Device,一次推送共享同一 DeviceId,
/// 因此 DeviceId 在 Command 顶层只出现一次。
/// </summary>
public record ReceiveInjectionPassCommand(
    Guid DeviceId,
    List<InjectionPassItemInput> Items
) : ICommand<Result<bool>>;

public record InjectionPassItemInput(
    string Barcode,
    string CellResult,
    DateTime CompletedTime,
    DateTime PreInjectionTime,
    decimal PreInjectionWeight,
    DateTime PostInjectionTime,
    decimal PostInjectionWeight,
    decimal InjectionVolume);

public class ReceiveInjectionPassHandler(
    IDeviceIdentityQueryService deviceIdentityQuery,
    IPublishEndpoint publishEndpoint
) : ICommandHandler<ReceiveInjectionPassCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        ReceiveInjectionPassCommand request,
        CancellationToken cancellationToken)
    {
        if (request.DeviceId == Guid.Empty)
            return Result.Failure("数据接收失败:DeviceId 不能为空");

        if (request.Items is null || request.Items.Count == 0)
            return Result.Failure("数据接收失败:过站数据列表不能为空");

        var exists = await deviceIdentityQuery.ExistsAsync(
            request.DeviceId, cancellationToken);

        if (!exists)
            return Result.Failure("数据接收失败:设备不存在");

        var @event = new PassDataInjectionReceivedEvent
        {
            DeviceId = request.DeviceId,
            Items = request.Items.Select(x => new PassDataInjectionItem
            {
                Barcode = x.Barcode,
                CellResult = x.CellResult,
                CompletedTime = x.CompletedTime.ToUniversalTime(),
                PreInjectionTime = x.PreInjectionTime.ToUniversalTime(),
                PreInjectionWeight = x.PreInjectionWeight,
                PostInjectionTime = x.PostInjectionTime.ToUniversalTime(),
                PostInjectionWeight = x.PostInjectionWeight,
                InjectionVolume = x.InjectionVolume
            }).ToList()
        };

        await publishEndpoint.Publish(@event, cancellationToken);

        return Result.Success(true);
    }
}