using AutoMapper;
using IIoT.Services.Common.Events;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Result;

namespace IIoT.ProductionService.Commands.PassStations;

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
    IPassStationReceiveService receiveService,
    IMapper mapper
) : ICommandHandler<ReceiveInjectionPassCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        ReceiveInjectionPassCommand request,
        CancellationToken cancellationToken)
    {
        var @event = mapper.Map<PassDataInjectionReceivedEvent>(request);
        return await receiveService.ValidateAndPublishAsync(
            request.DeviceId,
            request.Items?.Count ?? 0,
            @event,
            cancellationToken);
    }
}
