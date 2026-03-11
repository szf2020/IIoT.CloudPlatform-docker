using IIoT.Application.Contracts;
using IIoT.Core.Production.Aggregates.Devices;
using IIoT.Services.Common.Attributes;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Repository;
using IIoT.SharedKernel.Result;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IIoT.ProductionService.Commands.Devices;

/// <summary>
/// 业务指令：注册新物理设备
/// </summary>
// 🌟 第一道门：行为拦截。强制校验 Token 中是否含有 "Device.Create" 权限点
[AuthorizeRequirement("Device.Create")]
public record RegisterDeviceCommand(
    string DeviceName,
    string DeviceCode,
    string MacAddress,
    Guid ProcessId
) : ICommand<Result<Guid>>;

/// <summary>
/// 设备注册处理器
/// </summary>
public class RegisterDeviceHandler(
    IDataQueryService dataQueryService,   // 🌟 注入极速查询服务，专门用于防重校验 (绕过EF追踪)
    IRepository<Device> deviceRepository  // 🌟 注入写仓储，专门用于状态变更与落地
) : ICommandHandler<RegisterDeviceCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RegisterDeviceCommand request, CancellationToken cancellationToken)
    {
        // ==========================================
        // 🌟 1. 极速无锁校验区 (压榨性能)
        // ==========================================

        // 校验 A：指定的归属工序必须合法存在 (跨聚合根强校验)
        var processExists = await dataQueryService.AnyAsync(
            dataQueryService.MfgProcesses.Where(p => p.Id == request.ProcessId)
        );
        if (!processExists)
        {
            return Result.Failure("设备注册失败：指定的归属工序不存在");
        }

        // 校验 B：MAC 地址在全厂必须绝对唯一 (防伪核心)
        var macExists = await dataQueryService.AnyAsync(
            dataQueryService.Devices.Where(d => d.MacAddress == request.MacAddress)
        );
        if (macExists)
        {
            return Result.Failure($"设备注册失败：MAC地址 [{request.MacAddress}] 已被其他设备占用");
        }

        // 校验 C：设备系统编号必须唯一
        var codeExists = await dataQueryService.AnyAsync(
            dataQueryService.Devices.Where(d => d.DeviceCode == request.DeviceCode)
        );
        if (codeExists)
        {
            return Result.Failure($"设备注册失败：设备编号 [{request.DeviceCode}] 已存在");
        }

        // ==========================================
        // 🌟 2. 领域对象构建与持久化
        // ==========================================

        // 调用充血模型构造函数，强制满足聚合根的完整性约束
        var device = new Device(
            request.DeviceName,
            request.DeviceCode,
            request.MacAddress,
            request.ProcessId
        );

        // 业务落库
        deviceRepository.Add(device);
        await deviceRepository.SaveChangesAsync(cancellationToken);

        // 完美返回 Result.Success，并携带新生成设备的 Guid
        return Result.Success(device.Id);
    }
}