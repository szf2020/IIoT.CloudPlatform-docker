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
) : IQuery<Result<PagedList<InjectionPassListItemDto>>>;

public class GetInjectionLatest200ByDeviceHandler(
    IPassStationQueryService queryService
) : IQueryHandler<GetInjectionLatest200ByDeviceQuery, Result<PagedList<InjectionPassListItemDto>>>
{
    public async Task<Result<PagedList<InjectionPassListItemDto>>> Handle(GetInjectionLatest200ByDeviceQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await queryService.GetInjectionLatest200ByDeviceAsync(
            request.DeviceId,
            request.PaginationParams,
            cancellationToken);

        var pagedList = new PagedList<InjectionPassListItemDto>(items, totalCount, request.PaginationParams);
        return Result.Success(pagedList);
    }
}
