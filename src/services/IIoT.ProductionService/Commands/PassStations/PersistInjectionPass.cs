using IIoT.Core.Production.Contracts.RecordRepositories;
using IIoT.Services.Common.Contracts.DapperQueries;
using IIoT.Services.Common.Events;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Result;

namespace IIoT.ProductionService.Commands.PassStations;

/// <summary>
/// Persist command dispatched by DataWorker after message consumption.
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

        var exists = await deviceIdentityQuery.ExistsAsync(
            evt.DeviceId, cancellationToken);

        if (!exists)
            return Result.Failure($"Persist failed: DeviceId {evt.DeviceId} does not exist.");

        var receivedAt = DateTime.UtcNow;

        var writeModels = evt.Items.Select(item => new PassDataInjectionWriteModel(
            Id: Guid.NewGuid(),
            DeviceId: evt.DeviceId,
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
