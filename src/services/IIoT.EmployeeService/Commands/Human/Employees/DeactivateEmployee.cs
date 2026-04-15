using IIoT.Core.Employees.Aggregates.Employees;
using IIoT.Core.Employees.Specifications;
using IIoT.Services.Common.Caching;
using IIoT.Services.Common.Attributes;
using IIoT.Services.Common.Contracts;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Repository;
using IIoT.SharedKernel.Result;

namespace IIoT.EmployeeService.Commands.Employees;

[AuthorizeRequirement("Employee.Deactivate")]
[DistributedLock("iiot:lock:employee:{EmployeeId}", TimeoutSeconds = 5)]
public record DeactivateEmployeeCommand(Guid EmployeeId) : IHumanCommand<Result>;

public class DeactivateEmployeeHandler(
    IRepository<Employee> employeeRepository,
    IIdentityAccountStore identityAccountStore,
    IUnitOfWork unitOfWork,
    ICacheService cacheService)
    : ICommandHandler<DeactivateEmployeeCommand, Result>
{
    public async Task<Result> Handle(
        DeactivateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);

            var employee = await employeeRepository.GetSingleOrDefaultAsync(
                new EmployeeWithAccessesSpec(request.EmployeeId),
                cancellationToken);

            if (employee is null)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure("未找到目标员工档案");
            }

            if (employee.IsActive)
            {
                employee.Deactivate();
                employeeRepository.Update(employee);
                await employeeRepository.SaveChangesAsync(cancellationToken);
            }

            var identityResult = await identityAccountStore.SetEnabledAsync(
                request.EmployeeId,
                false,
                cancellationToken);

            if (!identityResult.IsSuccess)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure(identityResult.Errors?.ToArray() ?? ["员工身份账号停用失败"]);
            }

            await cacheService.RemoveAsync(CacheKeys.DeviceAccessesByUser(request.EmployeeId), cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            return Result.Failure($"员工停用失败: {ex.Message}");
        }
    }
}
