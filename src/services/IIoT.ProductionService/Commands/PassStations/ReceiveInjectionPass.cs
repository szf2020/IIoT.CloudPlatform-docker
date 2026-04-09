using IIoT.Services.Common.Contracts;
using IIoT.Services.Common.Events;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Result;
using AutoMapper;
using MassTransit;

namespace IIoT.ProductionService.Commands.PassStations;

/// <summary>
/// 业务指令:接收注液工序过站数据(单条)。
/// 上位机推送到 HttpApi → 校验设备存在且活跃 → 发布到 MQ → DataWorker 消费落库。
/// 上位机身份已在轮询认证接口换取 DeviceId,后续数据流转全部以 DeviceId 为唯一标识。
/// </summary>
public record ReceiveInjectionPassCommand(
    Guid DeviceId,
    string Barcode,
    string CellResult,
    DateTime CompletedTime,
    DateTime PreInjectionTime,
    decimal PreInjectionWeight,
    DateTime PostInjectionTime,
    decimal PostInjectionWeight,
    decimal InjectionVolume
) : ICommand<Result<bool>>;

public class ReceiveInjectionPassHandler(
    IDataQueryService dataQueryService,
    IMapper mapper,
    IPublishEndpoint publishEndpoint
) : ICommandHandler<ReceiveInjectionPassCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        ReceiveInjectionPassCommand request,
        CancellationToken cancellationToken)
    {
        if (request.DeviceId == Guid.Empty)
            return Result.Failure("数据接收失败:DeviceId 不能为空");

        var deviceExists = await dataQueryService.AnyAsync(
            dataQueryService.Devices.Where(d => d.Id == request.DeviceId && d.IsActive));

        if (!deviceExists)
            return Result.Failure("数据接收失败:设备不存在或已停用");

        // 边缘端时间戳统一转 UTC(Npgsql timestamptz 仅接受 Kind=Utc)
        var utcRequest = request with
        {
            CompletedTime = request.CompletedTime.ToUniversalTime(),
            PreInjectionTime = request.PreInjectionTime.ToUniversalTime(),
            PostInjectionTime = request.PostInjectionTime.ToUniversalTime(),
        };

        var @event = mapper.Map<PassDataInjectionReceivedEvent>(utcRequest);
        await publishEndpoint.Publish(@event, cancellationToken);

        return Result.Success(true);
    }
}