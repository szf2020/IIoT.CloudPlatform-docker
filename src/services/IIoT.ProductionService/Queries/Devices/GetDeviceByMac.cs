using IIoT.Core.Production.Aggregates.Devices;
using IIoT.Core.Production.Specifications.Devices;
using IIoT.Services.Common.Contracts;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Repository;
using IIoT.SharedKernel.Result;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IIoT.ProductionService.Queries.Devices;

public record DeviceIdentityDto(
    Guid Id,
    string DeviceName,
    Guid ProcessId,
    bool IsActive
);

public record GetDeviceByMacQuery(string MacAddress) : IQuery<Result<DeviceIdentityDto>>;

public class GetDeviceByMacHandler(
    IReadRepository<Device> deviceRepository,
    ICacheService cacheService
) : IQueryHandler<GetDeviceByMacQuery, Result<DeviceIdentityDto>>
{
    public async Task<Result<DeviceIdentityDto>> Handle(GetDeviceByMacQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"iiot:device:mac:v1:{request.MacAddress}";

        var cachedDto = await cacheService.GetAsync<DeviceIdentityDto>(cacheKey, cancellationToken);
        if (cachedDto != null) return Result.Success(cachedDto);

        var spec = new DeviceByMacSpec(request.MacAddress);
        var device = await deviceRepository.GetSingleOrDefaultAsync(spec, cancellationToken);

        if (device == null)
            return Result.Failure($"寻址失败：未找到物理 MAC 地址为 [{request.MacAddress}] 的设备注册档案");

        var dto = new DeviceIdentityDto(
            device.Id,
            device.DeviceName,
            device.ProcessId,
            device.IsActive
        );

        await cacheService.SetAsync(cacheKey, dto, TimeSpan.FromHours(2), cancellationToken);

        return Result.Success(dto);
    }
}