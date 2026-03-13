using IIoT.Core.Employee.Aggregates.MfgProcesses;
using IIoT.Core.Employee.Specifications;
using IIoT.Services.Common.Attributes;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Paging;
using IIoT.SharedKernel.Repository;
using IIoT.SharedKernel.Result;

namespace IIoT.EmployeeService.Queries.MfgProcesses;

/// <summary>
/// 纯净的工序列表展示 DTO
/// </summary>
public record MfgProcessListItemDto(
    Guid Id,
    string ProcessCode,
    string ProcessName
);

/// <summary>
/// 交互查询：获取工序分页列表 (带搜索)
/// </summary>
[AuthorizeRequirement("Process.Read")]
public record GetMfgProcessPagedListQuery(Pagination PaginationParams, string? Keyword = null) : IQuery<Result<PagedList<MfgProcessListItemDto>>>;

public class GetMfgProcessPagedListHandler(
    IReadRepository<MfgProcess> processRepository
) : IQueryHandler<GetMfgProcessPagedListQuery, Result<PagedList<MfgProcessListItemDto>>>
{
    public async Task<Result<PagedList<MfgProcessListItemDto>>> Handle(GetMfgProcessPagedListQuery request, CancellationToken cancellationToken)
    {
        var skip = (request.PaginationParams.PageNumber - 1) * request.PaginationParams.PageSize;
        var take = request.PaginationParams.PageSize;

        // 统计总数：不启用分页
        var countSpec = new MfgProcessPagedSpec(0, 0, request.Keyword, isPaging: false);
        var totalCount = await processRepository.CountAsync(countSpec, cancellationToken);

        // 先拿 count，再按需查数据，单个 DbContext 串行执行，绝不并发
        List<MfgProcess> list = [];
        if (totalCount > 0)
        {
            var pagedSpec = new MfgProcessPagedSpec(skip, take, request.Keyword, isPaging: true);
            list = await processRepository.GetListAsync(pagedSpec, cancellationToken);
        }

        var dtos = list.Select(p => new MfgProcessListItemDto(
            p.Id,
            p.ProcessCode,
            p.ProcessName
        )).ToList();

        var pagedList = new PagedList<MfgProcessListItemDto>(dtos, totalCount, request.PaginationParams);

        return Result.Success(pagedList);
    }
}
