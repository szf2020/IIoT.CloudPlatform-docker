using IIoT.Core.Production.Aggregates.Devices;
using IIoT.Core.Production.Specifications.Devices;
using IIoT.Services.Common.Attributes;
using IIoT.Services.Common.Contracts;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Repository;
using IIoT.SharedKernel.Result;

namespace IIoT.ProductionService.Commands.Devices;

/// <summary>
/// 删除设备(硬删除)。设备一旦删除,边缘端将无法从云端拉取配方。
/// </summary>
[AuthorizeRequirement("Device.Delete")]
public record DeleteDeviceCommand(Guid DeviceId) : ICommand<Result<bool>>;

public class DeleteDeviceHandler(
    IRepository<Device> deviceRepository,
    ICacheService cacheService
) : ICommandHandler<DeleteDeviceCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        DeleteDeviceCommand request,
        CancellationToken cancellationToken)
    {
        var device = await deviceRepository.GetSingleOrDefaultAsync(
            new DeviceByIdSpec(request.DeviceId),
            cancellationToken);

        if (device is null)
            return Result.Failure("目标设备不存在");

        deviceRepository.Delete(device);
        var affected = await deviceRepository.SaveChangesAsync(cancellationToken);

        if (affected > 0)
        {
            await cacheService.RemoveAsync(
                $"iiot:device:instance:v1:{device.Instance}", cancellationToken);
            await cacheService.RemoveAsync(
                $"iiot:devices:process:v1:{device.ProcessId}", cancellationToken);
            await cacheService.RemoveAsync("iiot:devices:v1:all-active", cancellationToken);
            await cacheService.RemoveAsync(
                $"iiot:device:identity:v1:{device.Id}", cancellationToken);
        }

        return Result.Success(true);
    }
}
