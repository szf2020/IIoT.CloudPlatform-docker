using IIoT.Application.Contracts;
using IIoT.Core.Employee.Aggregates.MfgProcesses;
using IIoT.Services.Common.Attributes;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Repository;
using IIoT.SharedKernel.Result;

namespace IIoT.EmployeeService.Commands.MfgProcesses;

/// <summary>
/// 业务指令：创建新的制造工序
/// </summary>
[AuthorizeRequirement("Process.Create")]
public record CreateMfgProcessCommand(
    string ProcessCode,
    string ProcessName
) : ICommand<Result<Guid>>;

public class CreateMfgProcessHandler(
    IDataQueryService dataQueryService,       // 🌟 极速无锁查询服务，用于防重校验 (绕过 EF 追踪)
    IRepository<MfgProcess> processRepository // 🌟 写仓储，用于状态变更与落地
) : ICommandHandler<CreateMfgProcessCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateMfgProcessCommand request, CancellationToken cancellationToken)
    {
        // ==========================================
        // 🌟 1. 极速无锁校验区 (压榨性能)
        // ==========================================

        // 校验：工序编码在全厂必须绝对唯一
        var codeExists = await dataQueryService.AnyAsync(
            dataQueryService.MfgProcesses.Where(p => p.ProcessCode == request.ProcessCode)
        );
        if (codeExists)
        {
            return Result.Failure($"工序创建失败：编码 [{request.ProcessCode}] 已存在");
        }

        // ==========================================
        // 🌟 2. 领域对象构建与持久化
        // ==========================================

        var process = new MfgProcess(request.ProcessCode, request.ProcessName);

        processRepository.Add(process);
        await processRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(process.Id);
    }
}
