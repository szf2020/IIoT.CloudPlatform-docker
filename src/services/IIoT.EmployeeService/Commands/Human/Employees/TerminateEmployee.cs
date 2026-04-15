using IIoT.Core.Employees.Aggregates.Employees;
using IIoT.Core.Employees.Specifications;
using IIoT.Services.Common.Attributes;
using IIoT.Services.Common.Contracts;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Repository;
using IIoT.SharedKernel.Result;

namespace IIoT.EmployeeService.Commands.Employees;

[AuthorizeRequirement("Employee.Terminate")]
[DistributedLock("iiot:lock:employee:{EmployeeId}", TimeoutSeconds = 5)]
public record TerminateEmployeeCommand(Guid EmployeeId) : IHumanCommand<Result>;

public class TerminateEmployeeHandler(
    IRepository<Employee> employeeRepository,
    IIdentityAccountStore identityAccountStore,
    IUnitOfWork unitOfWork)
    : ICommandHandler<TerminateEmployeeCommand, Result>
{
    public async Task<Result> Handle(
        TerminateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);

            var employee = await employeeRepository.GetSingleOrDefaultAsync(
                new EmployeeWithAccessesSpec(request.EmployeeId),
                cancellationToken);

            if (employee is not null)
            {
                employeeRepository.Delete(employee);
                await employeeRepository.SaveChangesAsync(cancellationToken);
            }

            var identityResult = await identityAccountStore.DeleteAsync(request.EmployeeId, cancellationToken);
            if (!identityResult.IsSuccess)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure(identityResult.Errors?.ToArray() ?? ["账号销毁失败"]);
            }

            await unitOfWork.CommitAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            return Result.Failure($"离职办理失败: {ex.Message}");
        }
    }
}

