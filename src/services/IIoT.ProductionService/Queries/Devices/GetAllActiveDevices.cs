using IIoT.Core.Production.Aggregates.Devices;
using IIoT.Core.Production.Specifications.Devices;
using IIoT.Services.Common.Attributes;
using IIoT.Services.Common.Contracts;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Repository;
using IIoT.SharedKernel.Result;

namespace IIoT.ProductionService.Queries.Devices;

public record DeviceSelectDto(
    Guid Id,
    string DeviceName,
    Guid ProcessId,
    bool IsActive
);

[AuthorizeRequirement("Device.Read")]
public record GetAllActiveDevicesQuery() : IQuery<Result<List<DeviceSelectDto>>>;

public class GetAllActiveDevicesHandler(
    IReadRepository<Device> deviceRepository,
    ICacheService cacheService
) : IQueryHandler<GetAllActiveDevicesQuery, Result<List<DeviceSelectDto>>>
{
    private const string CacheKey = "iiot:devices:v1:all-active";

    public async Task<Result<List<DeviceSelectDto>>> Handle(GetAllActiveDevicesQuery request, CancellationToken cancellationToken)
    {
        var cached = await cacheService.GetAsync<List<DeviceSelectDto>>(CacheKey, cancellationToken);
        if (cached != null) return Result.Success(cached);

        var spec = new DeviceAllActiveSpec();
        var list = await deviceRepository.GetListAsync(spec, cancellationToken);

        var dtos = list.Select(d => new DeviceSelectDto(
            d.Id, d.DeviceName, d.ProcessId, d.IsActive
        )).ToList();

        await cacheService.SetAsync(CacheKey, dtos, TimeSpan.FromHours(2), cancellationToken);

        return Result.Success(dtos);
    }
}