using IIoT.Core.Production.Contracts.RecordRepositories;
using IIoT.Services.Common.Contracts.DapperQueries;
using IIoT.Services.Common.Events;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Result;

namespace IIoT.ProductionService.Commands.DeviceLogs;

/// <summary>
/// Persist command dispatched by DataWorker after message consumption.
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

        var exists = await deviceIdentityQuery.ExistsAsync(
            evt.DeviceId, cancellationToken);

        if (!exists)
            return Result.Failure($"Persist failed: DeviceId {evt.DeviceId} does not exist.");

        var receivedAt = DateTime.UtcNow;

        var writeModels = evt.Logs.Select(item => new DeviceLogWriteModel(
            Id: Guid.NewGuid(),
            DeviceId: evt.DeviceId,
            Level: item.Level,
            Message: item.Message,
            LogTime: DateTime.SpecifyKind(item.LogTime, DateTimeKind.Utc),
            ReceivedAt: receivedAt
        )).ToList();

        await repository.InsertBatchAsync(writeModels, cancellationToken);

        return Result.Success(true);
    }
}
