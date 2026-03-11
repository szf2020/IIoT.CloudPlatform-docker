using IIoT.Application.Contracts;
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

[AuthorizeRequirement("Device.Update")] // 第一道门：基础行为权限
public record UpdateDeviceProfileCommand(
    Guid DeviceId,
    string DeviceName,
    string DeviceCode
) : ICommand<Result<bool>>;

public class UpdateDeviceProfileHandler(
    ICurrentUser currentUser,
    IReadRepository<Employee> employeeRepository, // 读取员工管辖权
    IRepository<Device> deviceRepository,         // 实体修改落地
    IDataQueryService dataQueryService,           // 防重校验
    ICacheService cacheService                    // 🌟 注入全局缓存服务
) : ICommandHandler<UpdateDeviceProfileCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(UpdateDeviceProfileCommand request, CancellationToken cancellationToken)
    {
        var device = await deviceRepository.GetByIdAsync(request.DeviceId, cancellationToken);
        if (device == null) return Result.Failure("目标设备不存在");

        // ==========================================
        // 🌟 第二道门：工序管辖权绝对拦截 (ABAC)
        // ==========================================
        if (currentUser.Role != "Admin") // Admin 拥有全厂上帝视角，直接放行
        {
            if (!Guid.TryParse(currentUser.Id, out var userId)) return Result.Failure("用户凭证异常");

            var employee = await employeeRepository.GetAsync(
                e => e.Id == userId,
                [e => e.ProcessAccesses], // 联级拉取该员工的【工序管辖权】
                cancellationToken);

            if (employee == null) return Result.Failure("系统中未找到您的员工档案");

            // 判断该设备所属的工序，是否在当前登录人的管辖列表中
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
            // 1. 删除该设备的单体缓存
            await cacheService.RemoveAsync($"iiot:device:v1:{device.Id}", cancellationToken);
            // 2. 删除该工序下的设备列表缓存 (如果有的情况下)
            await cacheService.RemoveAsync($"iiot:devices:process:v1:{device.ProcessId}", cancellationToken);
        }

        return Result.Success(true);
    }
}