using IIoT.Services.Common.Contracts;
using IIoT.Services.Common.Contracts.DapperQueries;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Result;

namespace IIoT.ProductionService.Queries.Capacities;

public record GetHourlyCapacityByDeviceQuery(
    Guid DeviceId,
    DateOnly StartDate,
    DateOnly EndDate
) : IQuery<Result<object>>;

public class GetHourlyCapacityByDeviceHandler(
    ICapacityQueryService capacityQueryService,
    ICacheService cacheService)
    : IQueryHandler<GetHourlyCapacityByDeviceQuery, Result<object>>
{
    public async Task<Result<object>> Handle(
        GetHourlyCapacityByDeviceQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"iiot:capacity:hourly:device:v1:{request.DeviceId}:{request.StartDate:yyyyMMdd}:{request.EndDate:yyyyMMdd}";

        var cached = await cacheService.GetAsync<object>(cacheKey, cancellationToken);
        if (cached != null)
            return Result.Success(cached);

        var items = await capacityQueryService.GetHourlyByDeviceAsync(
            request.DeviceId,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        var result = new
        {
            Items = items,
            Count = items.Count
        };

        await cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);

        return Result.Success((object)result);
    }
}
