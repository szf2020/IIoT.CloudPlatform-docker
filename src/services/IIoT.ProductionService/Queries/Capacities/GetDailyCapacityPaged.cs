using IIoT.Services.Common.Contracts;
using IIoT.Services.Common.Contracts.DapperQueries;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Paging;
using IIoT.SharedKernel.Result;

namespace IIoT.ProductionService.Queries.Capacities;

/// <summary>
/// 查询：所有机台产能分页加载（延迟加载，带设备名称和良率，缓存优先）
/// </summary>
public record GetDailyCapacityPagedQuery(
    Pagination PaginationParams,
    DateOnly? Date = null,
    Guid? DeviceId = null
) : IQuery<Result<object>>;

public class GetDailyCapacityPagedHandler(
    ICapacityQueryService queryService,
    ICacheService cacheService
) : IQueryHandler<GetDailyCapacityPagedQuery, Result<object>>
{
    public async Task<Result<object>> Handle(GetDailyCapacityPagedQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"iiot:capacity:paged:v1:{request.Date:yyyyMMdd}:{request.DeviceId}:{request.PaginationParams.PageNumber}:{request.PaginationParams.PageSize}";

        var cached = await cacheService.GetAsync<object>(cacheKey, cancellationToken);
        if (cached != null)
            return Result.Success(cached);

        var (items, totalCount) = await queryService.GetDailyPagedAsync(
            request.PaginationParams,
            request.Date,
            request.DeviceId,
            cancellationToken);

        var result = new
        {
            Items = items,
            MetaData = new
            {
                TotalCount = totalCount,
                PageSize = request.PaginationParams.PageSize,
                CurrentPage = request.PaginationParams.PageNumber,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PaginationParams.PageSize)
            }
        };

        await cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);

        return Result.Success<object>(result);
    }
}