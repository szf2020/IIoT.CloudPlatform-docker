using IIoT.Services.Common.Attributes;
using IIoT.Services.Common.Contracts;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Result;

namespace IIoT.IdentityService.Commands;

/// <summary>
/// 修改已有角色的权限点 (万一配错了或者业务变更时使用)
/// </summary>
[AuthorizeRequirement("Role.Update")]
public record UpdateRolePermissionsCommand(string RoleName, List<string> Permissions) : ICommand<Result<bool>>;

public class UpdateRolePermissionsHandler(
    IIdentityService identityService,
    ICacheService cacheService               // 🌟 注入缓存服务
) : ICommandHandler<UpdateRolePermissionsCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        // 🌟 防越权：坚决禁止通过接口篡改系统内置的 Admin 角色权限
        if (request.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure("系统保护：内置 Admin 角色的权限由系统硬编码，禁止修改！");
        }

        // 直接调用底层已经写好的"差集更新"算法
        var result = await identityService.UpdateRolePermissionsAsync(request.RoleName, request.Permissions);

        if (result.IsSuccess && result.Value)
        {
            // 🌟 缓存双杀：角色权限变更后，爆破权限定义缓存
            await cacheService.RemoveAsync("iiot:permissions:v1:all-defined", cancellationToken);
        }

        return result;
    }
}
