using IIoT.Core.Employee.Aggregates.Employees;
using IIoT.Core.Employee.Specifications;
using IIoT.Services.Common.Contracts;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Repository;
using IIoT.SharedKernel.Result;

namespace IIoT.IdentityService.Commands;

/// <summary>
/// 边缘端登录命令。
/// 非 Admin 用户需要通过设备绑定校验。
/// </summary>
public record DeviceLoginCommand(
    string EmployeeNo,
    string Password,
    Guid DeviceId
) : ICommand<Result<string>>;

public class DeviceLoginHandler(
    IAccountService accountService,
    IPermissionProvider permissionProvider,
    ICacheService cacheService,
    IJwtTokenGenerator jwtTokenGenerator,
    IReadRepository<Employee> employeeRepository)
    : ICommandHandler<DeviceLoginCommand, Result<string>>
{
    public async Task<Result<string>> Handle(
        DeviceLoginCommand request,
        CancellationToken cancellationToken)
    {
        // 校验账号密码。
        var checkResult = await accountService.CheckPasswordAsync(
            request.EmployeeNo, request.Password);

        if (!checkResult.IsSuccess || !checkResult.Value)
            return Result.Failure("工号不存在或密码错误");

        // 读取账号角色。
        var userId = await accountService.GetUserIdByEmployeeNoAsync(request.EmployeeNo);
        if (userId == null) return Result.Failure("身份信息异常");

        var roles = await accountService.GetRolesAsync(request.EmployeeNo);
        var isAdmin = roles.Contains("Admin");

        // 非管理员必须绑定目标设备。
        if (!isAdmin)
        {
            var spec = new EmployeeWithAccessesSpec(userId.Value);
            var employee = await employeeRepository.GetSingleOrDefaultAsync(
                spec, cancellationToken);

            if (employee == null)
                return Result.Failure("员工档案不存在");

            var hasDeviceAccess = employee.DeviceAccesses
                .Any(d => d.DeviceId == request.DeviceId);

            if (!hasDeviceAccess)
                return Result.Failure("您无权操作此设备，请联系管理员绑定设备权限");
        }

        // 刷新权限缓存。
        await cacheService.RemoveAsync(
            $"iiot:permissions:v1:user:{userId.Value}", cancellationToken);

        // 重新加载权限并签发 JWT。
        var permissions = await permissionProvider.GetPermissionsAsync(
            userId.Value, cancellationToken);

        var token = jwtTokenGenerator.GenerateToken(
            userId.Value, request.EmployeeNo, roles, permissions);

        return Result.Success(token);
    }
}
