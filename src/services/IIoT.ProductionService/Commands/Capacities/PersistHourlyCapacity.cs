using IIoT.Core.Production.Contracts.RecordRepositories;
using IIoT.Core.Production.ValueObjects;
using IIoT.Services.Common.Contracts.DapperQueries;
using IIoT.Services.Common.Events;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Result;

namespace IIoT.ProductionService.Commands.Capacities;

/// <summary>
/// Persist 用例:落库半小时产能记录。
/// 由 DataWorker 的 HourlyCapacityConsumer 通过 MediatR 派发。
/// 反查 Device 身份(走 FusionCache) → 组装 WriteModel →
/// 调 Repository 的 UpsertAsync 幂等写入。
/// </summary>
public record PersistHourlyCapacityCommand(
    HourlyCapacityReceivedEvent Event
) : ICommand<Result<bool>>;

public class PersistHourlyCapacityHandler(
    IDeviceIdentityQueryService deviceIdentityQuery,
    IHourlyCapacityRecordRepository repository
) : ICommandHandler<PersistHourlyCapacityCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        PersistHourlyCapacityCommand request,
        CancellationToken cancellationToken)
    {
        var evt = request.Event;

        var snapshot = await deviceIdentityQuery.GetByDeviceIdAsync(
            evt.DeviceId, cancellationToken);

        if (snapshot is null)
            return Result.Failure($"落库失败:DeviceId {evt.DeviceId} 对应的设备不存在");

        var writeModel = new HourlyCapacityWriteModel(
            Id: Guid.NewGuid(),
            DeviceId: evt.DeviceId,
            Instance: ClientInstanceId.Create(snapshot.MacAddress, snapshot.ClientCode),
            Date: evt.Date,
            ShiftCode: evt.ShiftCode,
            Hour: evt.Hour,
            Minute: evt.Minute,
            TimeLabel: evt.TimeLabel,
            TotalCount: evt.TotalCount,
            OkCount: evt.OkCount,
            NgCount: evt.NgCount,
            PlcName: evt.PlcName ?? string.Empty,
            ReportedAt: DateTime.UtcNow);

        await repository.UpsertAsync(writeModel, cancellationToken);

        return Result.Success(true);
    }
}
