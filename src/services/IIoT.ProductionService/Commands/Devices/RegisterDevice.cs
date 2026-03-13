using IIoT.Application.Contracts;
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
/// 业务指令：注册新物理设备
/// </summary>
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
    IDataQueryService dataQueryService,
    IRepository<Device> deviceRepository,
    ICacheService cacheService               // 🌟 注入缓存服务
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

        var device = new Device(
            request.DeviceName,
            request.DeviceCode,
            request.MacAddress,
            request.ProcessId
        );

        deviceRepository.Add(device);
        var affected = await deviceRepository.SaveChangesAsync(cancellationToken);

        // ==========================================
        // 🌟 3. 缓存一致性保障：新设备入库后爆破列表缓存
        // ==========================================
        if (affected > 0)
        {
            await cacheService.RemoveAsync("iiot:devices:v1:all-active", cancellationToken);
            await cacheService.RemoveAsync($"iiot:devices:process:v1:{device.ProcessId}", cancellationToken);
        }

        return Result.Success(device.Id);
    }
}
