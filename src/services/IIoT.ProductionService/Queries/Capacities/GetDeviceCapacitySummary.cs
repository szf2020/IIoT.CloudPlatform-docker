using IIoT.Services.Common.Contracts;
using IIoT.Services.Common.Contracts.DapperQueries;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Result;

namespace IIoT.ProductionService.Queries.Capacities;

/// <summary>
/// 查询：单机台产能汇总（指定日期范围，缓存优先）
/// </summary>
public record GetDeviceCapacitySummaryQuery(
    Guid DeviceId,
    DateOnly StartDate,
    DateOnly EndDate
) : IQuery<Result<object>>;

public class GetDeviceCapacitySummaryHandler(
    ICapacityQueryService queryService,
    ICacheService cacheService
) : IQueryHandler<GetDeviceCapacitySummaryQuery, Result<object>>
{
    public async Task<Result<object>> Handle(GetDeviceCapacitySummaryQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"iiot:capacity:summary:v1:{request.DeviceId}:{request.StartDate:yyyyMMdd}:{request.EndDate:yyyyMMdd}";

        var cached = await cacheService.GetAsync<List<dynamic>>(cacheKey, cancellationToken);
        if (cached != null)
            return Result.Success<object>(cached);

        var data = await queryService.GetDeviceSummaryAsync(
            request.DeviceId, request.StartDate, request.EndDate, cancellationToken);

        await cacheService.SetAsync(cacheKey, data, TimeSpan.FromMinutes(5), cancellationToken);

        return Result.Success<object>(data);
    }
}