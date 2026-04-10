using IIoT.Services.Common.Contracts;
using IIoT.Services.Common.Contracts.DapperQueries;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Paging;
using IIoT.SharedKernel.Result;

namespace IIoT.ProductionService.Queries.PassStations.Injection;

/// <summary>
/// 通用注液过站列表查询。合并 4 个追溯查询:
/// ByBarcodeAndProcess / ByTimeAndProcess / ByDeviceAndBarcode / ByDeviceAndTime。
/// 内部处理 ProcessId → deviceIds 的 EF 前置查询分支。
/// </summary>
public record GetInjectionListQuery(
    Pagination PaginationParams,
    Guid? ProcessId = null,
    Guid? DeviceId = null,
    string? Barcode = null,
    DateTime? StartTime = null,
    DateTime? EndTime = null
) : IQuery<Result<PagedList<InjectionPassListItemDto>>>;

public class GetInjectionListHandler(
    IDataQueryService dataQueryService,
    IPassStationQueryService queryService
) : IQueryHandler<GetInjectionListQuery, Result<PagedList<InjectionPassListItemDto>>>
{
    public async Task<Result<PagedList<InjectionPassListItemDto>>> Handle(
        GetInjectionListQuery request,
        CancellationToken cancellationToken)
    {
        List<Guid>? deviceIds = null;

        if (request.ProcessId.HasValue)
        {
            var devices = await dataQueryService.ToListAsync(
                dataQueryService.Devices.Where(d => d.ProcessId == request.ProcessId.Value));

            if (devices.Count == 0)
                return Result.Failure("该工序下没有设备");

            deviceIds = devices.Select(d => d.Id).ToList();
        }

        var (items, totalCount) = await queryService.GetInjectionByConditionAsync(
            request.PaginationParams,
            deviceIds: deviceIds,
            deviceId: request.DeviceId,
            barcode: request.Barcode,
            startTime: request.StartTime,
            endTime: request.EndTime,
            cancellationToken: cancellationToken);

        var pagedList = new PagedList<InjectionPassListItemDto>(
            items, totalCount, request.PaginationParams);

        return Result.Success(pagedList);
    }
}
