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

[AuthorizeRequirement("Device.Create")]
public record RegisterDeviceCommand(
    string DeviceName,
    string MacAddress,
    Guid ProcessId
) : ICommand<Result<Guid>>;

public class RegisterDeviceHandler(
    IDataQueryService dataQueryService,
    IRepository<Device> deviceRepository,
    ICacheService cacheService
) : ICommandHandler<RegisterDeviceCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RegisterDeviceCommand request, CancellationToken cancellationToken)
    {
        // 校验 A：指定的归属工序必须合法存在
        var processExists = await dataQueryService.AnyAsync(
            dataQueryService.MfgProcesses.Where(p => p.Id == request.ProcessId)
        );
        if (!processExists)
            return Result.Failure("设备注册失败：指定的归属工序不存在");

        // 校验 B：MAC 地址在全厂必须绝对唯一
        var macExists = await dataQueryService.AnyAsync(
            dataQueryService.Devices.Where(d => d.MacAddress == request.MacAddress)
        );
        if (macExists)
            return Result.Failure($"设备注册失败：MAC地址 [{request.MacAddress}] 已被其他设备占用");

        var device = new Device(
            request.DeviceName,
            request.MacAddress,
            request.ProcessId
        );

        deviceRepository.Add(device);
        var affected = await deviceRepository.SaveChangesAsync(cancellationToken);

        if (affected > 0)
        {
            await cacheService.RemoveAsync("iiot:devices:v1:all-active", cancellationToken);
            await cacheService.RemoveAsync($"iiot:devices:process:v1:{device.ProcessId}", cancellationToken);
        }

        return Result.Success(device.Id);
    }
}