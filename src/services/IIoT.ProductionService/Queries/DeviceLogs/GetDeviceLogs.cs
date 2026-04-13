using IIoT.Services.Common.Contracts.RecordQueries;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Paging;
using IIoT.SharedKernel.Result;

namespace IIoT.ProductionService.Queries.DeviceLogs;

/// <summary>
/// 设备日志条件查询。
/// 供日志控制器的多个筛选入口复用。
/// </summary>
public record GetDeviceLogsQuery(
    Pagination PaginationParams,
    Guid DeviceId,
    string? Level = null,
    string? Keyword = null,
    DateTime? StartTime = null,
    DateTime? EndTime = null
) : IQuery<Result<PagedList<DeviceLogListItemDto>>>;

public class GetDeviceLogsHandler(IDeviceLogQueryService queryService)
    : IQueryHandler<GetDeviceLogsQuery, Result<PagedList<DeviceLogListItemDto>>>
{
    public async Task<Result<PagedList<DeviceLogListItemDto>>> Handle(
        GetDeviceLogsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.DeviceId == Guid.Empty)
            return Result.Failure("DeviceId 不能为空");

        var (items, totalCount) = await queryService.GetLogsByConditionAsync(
            request.PaginationParams,
            request.DeviceId,
            request.Level,
            request.Keyword,
            request.StartTime,
            request.EndTime,
            cancellationToken);

        var pagedList = new PagedList<DeviceLogListItemDto>(
            items, totalCount, request.PaginationParams);

        return Result.Success(pagedList);
    }
}
