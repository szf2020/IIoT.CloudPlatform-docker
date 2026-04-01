using IIoT.Services.Common.Contracts;
using IIoT.Services.Common.Contracts.DapperQueries;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Paging;
using IIoT.SharedKernel.Result;

namespace IIoT.ProductionService.Queries.Capacities;

public record GetHourlyCapacityPagedQuery(
    Pagination Pagination,
    DateOnly? Date = null,
    Guid? DeviceId = null
) : IQuery<Result<object>>;

public class GetHourlyCapacityPagedHandler(
    ICapacityQueryService capacityQueryService,
    ICacheService cacheService)
    : IQueryHandler<GetHourlyCapacityPagedQuery, Result<object>>
{
    public async Task<Result<object>> Handle(
        GetHourlyCapacityPagedQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"iiot:capacity:hourly:paged:v1:{request.Date:yyyyMMdd}:{request.DeviceId}:{request.Pagination.PageNumber}:{request.Pagination.PageSize}";

        var cached = await cacheService.GetAsync<object>(cacheKey, cancellationToken);
        if (cached != null)
            return Result.Success(cached);

        var (items, totalCount) = await capacityQueryService.GetHourlyPagedAsync(
            request.Pagination,
            request.Date,
            request.DeviceId,
            cancellationToken);

        var result = new
        {
            Items = items,
            MetaData = new
            {
                TotalCount = totalCount,
                PageSize = request.Pagination.PageSize,
                CurrentPage = request.Pagination.PageNumber,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.Pagination.PageSize)
            }
        };

        await cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);

        return Result.Success((object)result);
    }
}
