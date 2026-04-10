using IIoT.Core.Production.Contracts.RecordRepositories;
using IIoT.Core.Production.ValueObjects;
using IIoT.Services.Common.Contracts.DapperQueries;
using IIoT.Services.Common.Events;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Result;

namespace IIoT.ProductionService.Commands.PassStations;

/// <summary>
/// Persist 用例:批量落库注液过站数据。
/// 由 DataWorker 的 PassDataInjectionConsumer 通过 MediatR 派发。
/// 整批共享同一 DeviceId,一次反查 Device 身份,一次 InsertBatch。
/// 幂等性由 SQL 层 ON CONFLICT (device_id, barcode, completed_time)
/// DO NOTHING 保证。
/// </summary>
public record PersistInjectionPassCommand(
    PassDataInjectionReceivedEvent Event
) : ICommand<Result<bool>>;

public class PersistInjectionPassHandler(
    IDeviceIdentityQueryService deviceIdentityQuery,
    IPassDataInjectionRecordRepository repository
) : ICommandHandler<PersistInjectionPassCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        PersistInjectionPassCommand request,
        CancellationToken cancellationToken)
    {
        var evt = request.Event;

        if (evt.Items.Count == 0)
            return Result.Success(true);

        var snapshot = await deviceIdentityQuery.GetByDeviceIdAsync(
            evt.DeviceId, cancellationToken);

        if (snapshot is null)
            return Result.Failure($"落库失败:DeviceId {evt.DeviceId} 对应的设备不存在");

        var instance = ClientInstanceId.Create(snapshot.MacAddress, snapshot.ClientCode);
        var receivedAt = DateTime.UtcNow;

        var writeModels = evt.Items.Select(item => new PassDataInjectionWriteModel(
            Id: Guid.NewGuid(),
            DeviceId: evt.DeviceId,
            Instance: instance,
            CellResult: item.CellResult,
            CompletedTime: item.CompletedTime,
            ReceivedAt: receivedAt,
            Barcode: item.Barcode,
            PreInjectionTime: item.PreInjectionTime,
            PreInjectionWeight: item.PreInjectionWeight,
            PostInjectionTime: item.PostInjectionTime,
            PostInjectionWeight: item.PostInjectionWeight,
            InjectionVolume: item.InjectionVolume
        )).ToList();

        await repository.InsertBatchAsync(writeModels, cancellationToken);

        return Result.Success(true);
    }
}
