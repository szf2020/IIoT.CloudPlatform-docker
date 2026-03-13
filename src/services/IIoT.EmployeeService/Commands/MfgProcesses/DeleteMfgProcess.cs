using IIoT.Application.Contracts;
using IIoT.Core.Employee.Aggregates.MfgProcesses;
using IIoT.Services.Common.Attributes;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Repository;
using IIoT.SharedKernel.Result;

namespace IIoT.EmployeeService.Commands.MfgProcesses;

/// <summary>
/// 业务指令：删除工序 (硬删除)
/// </summary>
/// <remarks>
/// 工序一旦被设备或员工管辖权引用，禁止删除，防止数据孤岛。
/// </remarks>
[AuthorizeRequirement("Process.Delete")]
public record DeleteMfgProcessCommand(Guid ProcessId) : ICommand<Result<bool>>;

public class DeleteMfgProcessHandler(
    IDataQueryService dataQueryService,        // 极速无锁校验关联数据
    IRepository<MfgProcess> processRepository  // 写仓储
) : ICommandHandler<DeleteMfgProcessCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteMfgProcessCommand request, CancellationToken cancellationToken)
    {
        // 1. 查出待删除的工序实体
        var process = await processRepository.GetByIdAsync(request.ProcessId, cancellationToken);

        if (process == null)
        {
            return Result.Failure("未找到目标工序档案");
        }

        // ==========================================
        // 🌟 2. 关联数据安全校验 (防止数据孤岛)
        // ==========================================

        // 校验 A：是否有设备挂载在此工序下
        var deviceBound = await dataQueryService.AnyAsync(
            dataQueryService.Devices.Where(d => d.ProcessId == request.ProcessId)
        );
        if (deviceBound)
        {
            return Result.Failure("删除失败：该工序下仍有设备挂载，请先迁移或停用相关设备");
        }

        // 校验 B：是否有配方关联此工序
        var recipeBound = await dataQueryService.AnyAsync(
            dataQueryService.Recipes.Where(r => r.ProcessId == request.ProcessId)
        );
        if (recipeBound)
        {
            return Result.Failure("删除失败：该工序下仍有配方关联，请先停用或迁移相关配方");
        }

        // 3. 安全删除
        processRepository.Delete(process);
        await processRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
