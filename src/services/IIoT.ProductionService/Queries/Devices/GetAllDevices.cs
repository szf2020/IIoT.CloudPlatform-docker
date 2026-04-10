using IIoT.Core.Production.Aggregates.Devices;
using IIoT.Services.Common.Attributes;
using IIoT.Services.Common.Contracts;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Repository;
using IIoT.SharedKernel.Result;

namespace IIoT.ProductionService.Queries.Devices;

public record DeviceSelectDto(
    Guid Id,
    string DeviceName,
    Guid ProcessId
);

[AuthorizeRequirement("Device.Read")]
public record GetAllDevicesQuery() : IQuery<Result<List<DeviceSelectDto>>>;

public class GetAllDevicesHandler(
    IReadRepository<Device> deviceRepository,
    ICacheService cacheService
) : IQueryHandler<GetAllDevicesQuery, Result<List<DeviceSelectDto>>>
{
    private const string CacheKey = "iiot:devices:v1:all-active";

    public async Task<Result<List<DeviceSelectDto>>> Handle(GetAllDevicesQuery request, CancellationToken cancellationToken)
    {
        var cached = await cacheService.GetAsync<List<DeviceSelectDto>>(CacheKey, cancellationToken);
        if (cached != null) return Result.Success(cached);

        var list = await deviceRepository.GetListAsync(cancellationToken: cancellationToken);

        var dtos = list.Select(d => new DeviceSelectDto(
            d.Id, d.DeviceName, d.ProcessId
        )).ToList();

        await cacheService.SetAsync(CacheKey, dtos, TimeSpan.FromHours(2), cancellationToken);

        return Result.Success(dtos);
    }
}
