using IIoT.Services.Common.Contracts;
using IIoT.Services.Common.Contracts.RecordQueries;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Paging;
using IIoT.SharedKernel.Result;

namespace IIoT.ProductionService.Queries.PassStations;

public record GetPassStationListQuery<TDto>(
    Pagination PaginationParams,
    Guid? ProcessId = null,
    Guid? DeviceId = null,
    string? Barcode = null,
    DateTime? StartTime = null,
    DateTime? EndTime = null
) : IQuery<Result<PagedList<TDto>>>;

public sealed class GetPassStationListHandler<TDto>(
    IDataQueryService dataQueryService,
    IPassStationQueryService<TDto> queryService)
    : IQueryHandler<GetPassStationListQuery<TDto>, Result<PagedList<TDto>>>
{
    public async Task<Result<PagedList<TDto>>> Handle(
        GetPassStationListQuery<TDto> request,
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

        var (items, totalCount) = await queryService.GetByConditionAsync(
            request.PaginationParams,
            deviceIds: deviceIds,
            deviceId: request.DeviceId,
            barcode: request.Barcode,
            startTime: request.StartTime,
            endTime: request.EndTime,
            cancellationToken: cancellationToken);

        return Result.Success(new PagedList<TDto>(items, totalCount, request.PaginationParams));
    }
}

public record GetPassStationDetailQuery<TDto>(Guid Id) : IQuery<Result<TDto>>;

public sealed class GetPassStationDetailHandler<TDto>(
    IPassStationQueryService<TDto> queryService)
    : IQueryHandler<GetPassStationDetailQuery<TDto>, Result<TDto>>
{
    public async Task<Result<TDto>> Handle(
        GetPassStationDetailQuery<TDto> request,
        CancellationToken cancellationToken)
    {
        var detail = await queryService.GetDetailAsync(request.Id, cancellationToken);
        return detail is null
            ? Result.Failure("未找到该过站记录")
            : Result.Success(detail);
    }
}

public record GetPassStationLatest200Query<TDto>(
    Guid DeviceId,
    Pagination PaginationParams
) : IQuery<Result<PagedList<TDto>>>;

public sealed class GetPassStationLatest200Handler<TDto>(
    IPassStationQueryService<TDto> queryService)
    : IQueryHandler<GetPassStationLatest200Query<TDto>, Result<PagedList<TDto>>>
{
    public async Task<Result<PagedList<TDto>>> Handle(
        GetPassStationLatest200Query<TDto> request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await queryService.GetLatest200ByDeviceAsync(
            request.DeviceId,
            request.PaginationParams,
            cancellationToken);

        return Result.Success(new PagedList<TDto>(items, totalCount, request.PaginationParams));
    }
}
