using IIoT.Core.Employee.Aggregates.Employees;
using IIoT.Core.Production.Aggregates.Devices;
using IIoT.Services.Common.Attributes;
using IIoT.Services.Common.Contracts;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Repository;
using IIoT.SharedKernel.Result;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IIoT.ProductionService.Commands.Devices;

/// <summary>
/// 软删除/停用设备 (设备一旦停用，将无法从云端拉取配方)
/// </summary>
[AuthorizeRequirement("Device.Deactivate")]
public record DeactivateDeviceCommand(Guid DeviceId) : ICommand<Result<bool>>;

public class DeactivateDeviceHandler(
    ICurrentUser currentUser,
    IReadRepository<Employee> employeeRepository,
    IRepository<Device> deviceRepository,
    ICacheService cacheService
) : ICommandHandler<DeactivateDeviceCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeactivateDeviceCommand request, CancellationToken cancellationToken)
    {
        var device = await deviceRepository.GetByIdAsync(request.DeviceId, cancellationToken);
        if (device == null) return Result.Failure("目标设备不存在");

        // 已经是停用状态，直接返回成功 (幂等性)
        if (!device.IsActive) return Result.Success(true);

        // 🌟 第二道门：ABAC 管辖权拦截
        if (currentUser.Role != "Admin")
        {
            var userId = Guid.Parse(currentUser.Id!);
            var employee = await employeeRepository.GetAsync(
                e => e.Id == userId,
                [e => e.ProcessAccesses],
                cancellationToken);

            var hasAccess = employee!.ProcessAccesses.Any(pa => pa.ProcessId == device.ProcessId);
            if (!hasAccess) return Result.Failure("越权警告：您无权停用其他车间/工序的设备！");
        }

        device.IsActive = false;

        deviceRepository.Update(device);
        var affected = await deviceRepository.SaveChangesAsync(cancellationToken);

        // 🌟 缓存爆破：停用的设备绝不能再被命中
        if (affected > 0)
        {
            await cacheService.RemoveAsync($"iiot:device:v1:{device.Id}", cancellationToken);
            await cacheService.RemoveAsync($"iiot:devices:process:v1:{device.ProcessId}", cancellationToken);
            await cacheService.RemoveAsync("iiot:devices:v1:all-active", cancellationToken);
        }

        return Result.Success(true);
    }
}
