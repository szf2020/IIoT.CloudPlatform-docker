using IIoT.Core.Employee.Aggregates.Employees;
using IIoT.Core.Production.Aggregates.Devices;
using IIoT.Services.Common.Attributes;
using IIoT.Services.Common.Contracts;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Repository;
using IIoT.SharedKernel.Result;

namespace IIoT.ProductionService.Commands.Devices;

[AuthorizeRequirement("Device.Update")]
public record UpdateDeviceProfileCommand(
    Guid DeviceId,
    string DeviceName
) : ICommand<Result<bool>>;

public class UpdateDeviceProfileHandler(
    ICurrentUser currentUser,
    IReadRepository<Employee> employeeRepository,
    IRepository<Device> deviceRepository,
    ICacheService cacheService
) : ICommandHandler<UpdateDeviceProfileCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(UpdateDeviceProfileCommand request, CancellationToken cancellationToken)
    {
        var device = await deviceRepository.GetByIdAsync(request.DeviceId, cancellationToken);
        if (device == null) return Result.Failure("目标设备不存在");

        if (currentUser.Role != "Admin")
        {
            if (!Guid.TryParse(currentUser.Id, out var userId)) return Result.Failure("用户凭证异常");

            var employee = await employeeRepository.GetAsync(
                e => e.Id == userId,
                [e => e.ProcessAccesses],
                cancellationToken);

            if (employee == null) return Result.Failure("系统中未找到您的员工档案");

            var hasAccess = employee.ProcessAccesses.Any(pa => pa.ProcessId == device.ProcessId);
            if (!hasAccess) return Result.Failure("越权警告：您没有该设备所属工序的管理权限，禁止修改！");
        }

        device.DeviceName = request.DeviceName;

        deviceRepository.Update(device);
        var affected = await deviceRepository.SaveChangesAsync(cancellationToken);

        if (affected > 0)
        {
            await cacheService.RemoveAsync($"iiot:device:mac:v1:{device.MacAddress}", cancellationToken);
            await cacheService.RemoveAsync($"iiot:devices:process:v1:{device.ProcessId}", cancellationToken);
            await cacheService.RemoveAsync("iiot:devices:v1:all-active", cancellationToken);
        }

        return Result.Success(true);
    }
}