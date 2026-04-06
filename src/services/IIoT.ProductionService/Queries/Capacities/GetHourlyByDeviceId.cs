using IIoT.Services.Common.Contracts;
using IIoT.Services.Common.Contracts.DapperQueries;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Result;

namespace IIoT.ProductionService.Queries.Capacities;

/// <summary>
/// 按日查询半小时明细（按 deviceId 查询，统一唯一标识）
/// plcName 可选，传入时只返回该 PLC 的数据
/// </summary>
public record GetHourlyByDeviceIdQuery(
    Guid DeviceId,
    DateOnly Date,
    string? PlcName = null
) : IQuery<Result<List<HourlyCapacityDto>>>;

public class GetHourlyByDeviceIdHandler(
    ICapacityQueryService queryService,
    ICacheService cacheService
) : IQueryHandler<GetHourlyByDeviceIdQuery, Result<List<HourlyCapacityDto>>>
{
    public async Task<Result<List<HourlyCapacityDto>>> Handle(
        GetHourlyByDeviceIdQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"iiot:capacity:hourly:v1:{request.DeviceId}:{request.Date:yyyyMMdd}:{request.PlcName ?? "all"}";

        var cached = await cacheService.GetAsync<List<HourlyCapacityDto>>(cacheKey, cancellationToken);
        if (cached is not null)
            return Result.Success(cached);

        var data = await queryService.GetHourlyByDeviceIdAsync(
            request.DeviceId,
            request.Date,
            request.PlcName,
            cancellationToken);

        if (data.Count > 0)
            await cacheService.SetAsync(cacheKey, data, TimeSpan.FromMinutes(5), cancellationToken);

        return Result.Success(data);
    }
}