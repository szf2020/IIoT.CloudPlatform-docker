using IIoT.Services.Common.Contracts;
using IIoT.Services.Common.Contracts.RecordQueries;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Result;

namespace IIoT.ProductionService.Queries.Capacities;

/// <summary>
/// 查询指定设备在日期范围内的每日汇总产能。
/// 传入 plcName 时只汇总对应 PLC 的数据。
/// </summary>
public record GetSummaryRangeQuery(
    Guid DeviceId,
    DateOnly StartDate,
    DateOnly EndDate,
    string? PlcName = null
) : IQuery<Result<List<DailyRangeSummaryDto>>>;

public class GetSummaryRangeHandler(
    ICapacityQueryService queryService,
    ICacheService cacheService
) : IQueryHandler<GetSummaryRangeQuery, Result<List<DailyRangeSummaryDto>>>
{
    public async Task<Result<List<DailyRangeSummaryDto>>> Handle(
        GetSummaryRangeQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"iiot:capacity:range:v1:{request.DeviceId}:{request.StartDate:yyyyMMdd}:{request.EndDate:yyyyMMdd}:{request.PlcName ?? "all"}";

        var cached = await cacheService.GetAsync<List<DailyRangeSummaryDto>>(cacheKey, cancellationToken);
        if (cached is not null)
            return Result.Success(cached);

        var data = await queryService.GetSummaryRangeAsync(
            request.DeviceId,
            request.StartDate,
            request.EndDate,
            request.PlcName,
            cancellationToken);

        if (data.Count > 0)
            await cacheService.SetAsync(cacheKey, data, TimeSpan.FromMinutes(5), cancellationToken);

        return Result.Success(data);
    }
}
