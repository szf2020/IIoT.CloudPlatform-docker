using IIoT.Core.Employee.Aggregates.MfgProcesses;
using IIoT.Services.Common.Attributes;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Repository;
using IIoT.SharedKernel.Result;

namespace IIoT.EmployeeService.Queries.MfgProcesses;

/// <summary>
/// 轻量 DTO：用于下拉选择器的工序简要信息
/// </summary>
public record MfgProcessSelectDto(
    Guid Id,
    string ProcessCode,
    string ProcessName
);

/// <summary>
/// 交互查询：获取全量工序列表 (供设备注册、配方创建、员工管辖权等下拉选择器使用)
/// </summary>
/// <remarks>
/// 工序数量在工厂场景下通常不超过百条，无需分页，直接全量拉取即可。
/// </remarks>
[AuthorizeRequirement("Process.Read")]
public record GetAllMfgProcessesQuery() : IQuery<Result<List<MfgProcessSelectDto>>>;

public class GetAllMfgProcessesHandler(
    IReadRepository<MfgProcess> processRepository
) : IQueryHandler<GetAllMfgProcessesQuery, Result<List<MfgProcessSelectDto>>>
{
    public async Task<Result<List<MfgProcessSelectDto>>> Handle(GetAllMfgProcessesQuery request, CancellationToken cancellationToken)
    {
        // 全量拉取，按编码排序
        var list = await processRepository.GetListAsync(p => true, cancellationToken);

        var dtos = list
            .OrderBy(p => p.ProcessCode)
            .Select(p => new MfgProcessSelectDto(p.Id, p.ProcessCode, p.ProcessName))
            .ToList();

        return Result.Success(dtos);
    }
}
