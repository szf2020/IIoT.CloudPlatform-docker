using IIoT.Core.Employee.Aggregates.Employees;
using IIoT.Services.Common.Contracts;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Repository;
using IIoT.SharedKernel.Result;

namespace IIoT.IdentityService.Commands;

public record CreateEmployeeCommand(string EmployeeNo, string RealName, string Password) : ICommand<Result<string>>;

public class CreateEmployeeHandler(
    IIdentityService identityService, // 🌟 纯洁的接口
    IRepository<Employee> employeeRepository)
    : ICommandHandler<CreateEmployeeCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var sharedId = Guid.NewGuid();

        // 1. 调用底层抽象接口创建保安身份
        var identityResult = await identityService.CreateUserAsync(sharedId, request.EmployeeNo, request.Password);
        if (!identityResult.IsSuccess)
        {
            return Result.Failure(identityResult.Errors?.ToArray() ?? ["账号创建失败"]);
        }
        // 🌟 只需要传入灵魂绑定的 sharedId, 工号 和 姓名！干干净净！
        var employee = new Employee(sharedId, request.EmployeeNo, request.RealName);

        employeeRepository.Add(employee);
        await employeeRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(sharedId.ToString());
    }
}