using IIoT.Services.Common.Contracts.DapperQueries;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Paging;
using IIoT.SharedKernel.Result;

namespace IIoT.ProductionService.Queries.PassStations.Injection;

/// <summary>
/// 查询：按机台查最近 200 条注液过站数据（无需填时间范围）
/// </summary>
public record GetInjectionLatest200ByDeviceQuery(
    Guid DeviceId,
    Pagination PaginationParams
) : IQuery<Result<object>>;

public class GetInjectionLatest200ByDeviceHandler(
    IPassStationQueryService queryService
) : IQueryHandler<GetInjectionLatest200ByDeviceQuery, Result<object>>
{
    public async Task<Result<object>> Handle(GetInjectionLatest200ByDeviceQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await queryService.GetInjectionLatest200ByDeviceAsync(
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
