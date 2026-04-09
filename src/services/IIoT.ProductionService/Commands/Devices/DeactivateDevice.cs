using IIoT.Core.Employee.Aggregates.Employees;
using IIoT.Core.Employee.Specifications;
using IIoT.Core.Production.Aggregates.Devices;
using IIoT.Core.Production.Specifications.Devices;
using IIoT.Services.Common.Attributes;
using IIoT.Services.Common.Contracts;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Repository;
using IIoT.SharedKernel.Result;

namespace IIoT.ProductionService.Commands.Devices;

/// <summary>
/// 停用设备(软删除)。设备一旦停用,边缘端将无法从云端拉取配方。
/// </summary>
[AuthorizeRequirement("Device.Deactivate")]
public record DeactivateDeviceCommand(Guid DeviceId) : ICommand<Result<bool>>;

public class DeactivateDeviceHandler(
    IRepository<Device> deviceRepository,
    ICacheService cacheService
) : ICommandHandler<DeactivateDeviceCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        DeactivateDeviceCommand request,
        CancellationToken cancellationToken)
    {
        var device = await deviceRepository.GetSingleOrDefaultAsync(
            new DeviceByIdSpec(request.DeviceId),
            cancellationToken);

        if (device is null)
            return Result.Failure("目标设备不存在");

        // 已停用直接返回成功(幂等)
        if (!device.IsActive)
            return Result.Success(true);

        device.Deactivate();

        deviceRepository.Update(device);
        var affected = await deviceRepository.SaveChangesAsync(cancellationToken);

        if (affected > 0)
        {
            await cacheService.RemoveAsync(
                $"iiot:device:instance:v1:{device.Instance}", cancellationToken);
            await cacheService.RemoveAsync(
                $"iiot:devices:process:v1:{device.ProcessId}", cancellationToken);
            await cacheService.RemoveAsync("iiot:devices:v1:all-active", cancellationToken);
        }

        return Result.Success(true);
    }
}