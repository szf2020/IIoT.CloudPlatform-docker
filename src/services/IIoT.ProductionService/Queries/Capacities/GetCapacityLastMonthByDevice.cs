using IIoT.Services.Common.Contracts.DapperQueries;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Paging;
using IIoT.SharedKernel.Result;

namespace IIoT.ProductionService.Queries.Capacities;

/// <summary>
/// 查询：按机台查最近一个月产能数据（无需手填日期范围）
/// </summary>
public record GetCapacityLastMonthByDeviceQuery(
    Guid DeviceId,
    Pagination PaginationParams
) : IQuery<Result<object>>;

public class GetCapacityLastMonthByDeviceHandler(
    ICapacityQueryService queryService
) : IQueryHandler<GetCapacityLastMonthByDeviceQuery, Result<object>>
{
    public async Task<Result<object>> Handle(GetCapacityLastMonthByDeviceQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await queryService.GetLastMonthByDeviceAsync(
            request.DeviceId,
            request.PaginationParams,
            cancellationToken);

        return Result.Success<object>(new
        {
            Items = items,
            MetaData = new
            {
                TotalCount  = totalCount,
                PageSize    = request.PaginationParams.PageSize,
                CurrentPage = request.PaginationParams.PageNumber,
                TotalPages  = (int)Math.Ceiling(totalCount / (double)request.PaginationParams.PageSize)
            }
        });
    }
}
