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

[AuthorizeRequirement("Device.Update")]
public record UpdateDeviceProfileCommand(
    Guid DeviceId,
    string DeviceName,
    string DeviceCode
) : ICommand<Result<bool>>;

public class UpdateDeviceProfileHandler(
    ICurrentUser currentUser,
    IReadRepository<Employee> employeeRepository,
    IRepository<Device> deviceRepository,
    IDataQueryService dataQueryService,
    ICacheService cacheService
) : ICommandHandler<UpdateDeviceProfileCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(UpdateDeviceProfileCommand request, CancellationToken cancellationToken)
    {
        var device = await deviceRepository.GetByIdAsync(request.DeviceId, cancellationToken);
        if (device == null) return Result.Failure("目标设备不存在");

        // ==========================================
        // 🌟 第二道门：工序管辖权绝对拦截 (ABAC)
        // ==========================================
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

        // ==========================================
        // 🌟 业务逻辑与唯一性校验
        // ==========================================
        if (device.DeviceCode != request.DeviceCode)
        {
            var codeExists = await dataQueryService.AnyAsync(
                dataQueryService.Devices.Where(d => d.DeviceCode == request.DeviceCode && d.Id != request.DeviceId)
            );
            if (codeExists) return Result.Failure($"修改失败：设备编号 [{request.DeviceCode}] 已被占用");
        }

        device.DeviceName = request.DeviceName;
        device.DeviceCode = request.DeviceCode;

        deviceRepository.Update(device);
        var affected = await deviceRepository.SaveChangesAsync(cancellationToken);

        // ==========================================
        // 🌟 缓存强一致性保障：写后即删
        // ==========================================
        if (affected > 0)
        {
            await cacheService.RemoveAsync($"iiot:device:v1:{device.Id}", cancellationToken);
            await cacheService.RemoveAsync($"iiot:devices:process:v1:{device.ProcessId}", cancellationToken);
            await cacheService.RemoveAsync("iiot:devices:v1:all-active", cancellationToken);
        }

        return Result.Success(true);
    }
}
