using IIoT.Core.Production.Contracts.RecordRepositories;
using IIoT.Core.Production.ValueObjects;
using IIoT.Services.Common.Contracts.DapperQueries;
using IIoT.Services.Common.Events;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Result;

namespace IIoT.ProductionService.Commands.DeviceLogs;

/// <summary>
/// Persist 用例:批量落库设备日志。
/// 由 DataWorker 的 DeviceLogConsumer 通过 MediatR 派发。
/// 整批共享同一 DeviceId,一次反查 Device 身份,一次 InsertBatch。
/// </summary>
public record PersistDeviceLogCommand(
    DeviceLogReceivedEvent Event
) : ICommand<Result<bool>>;

public class PersistDeviceLogHandler(
    IDeviceIdentityQueryService deviceIdentityQuery,
    IDeviceLogRecordRepository repository
) : ICommandHandler<PersistDeviceLogCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        PersistDeviceLogCommand request,
        CancellationToken cancellationToken)
    {
        var evt = request.Event;

        if (evt.Logs.Count == 0)
            return Result.Success(true);

        var snapshot = await deviceIdentityQuery.GetByDeviceIdAsync(
            evt.DeviceId, cancellationToken);

        if (snapshot is null)
            return Result.Failure($"落库失败:DeviceId {evt.DeviceId} 对应的设备不存在");

        var instance = ClientInstanceId.Create(snapshot.MacAddress, snapshot.ClientCode);
        var receivedAt = DateTime.UtcNow;

        var writeModels = evt.Logs.Select(item => new DeviceLogWriteModel(
            Id: Guid.NewGuid(),
            DeviceId: evt.DeviceId,
            Instance: instance,
            Level: item.Level,
            Message: item.Message,
            LogTime: DateTime.SpecifyKind(item.LogTime, DateTimeKind.Utc),
            ReceivedAt: receivedAt
        )).ToList();

        await repository.InsertBatchAsync(writeModels, cancellationToken);

        return Result.Success(true);
    }
}
