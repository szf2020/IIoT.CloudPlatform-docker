using IIoT.Services.Common.Attributes;
using IIoT.Services.Common.Contracts;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Result;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IIoT.IdentityService.Commands;

/// <summary>
/// 定义岗位权限策略：创建一个角色，并告诉保安这个角色能进哪些门
/// </summary>
[AuthorizeRequirement("Role.Define")]
public record DefineRolePolicyCommand(string RoleName, List<string> Permissions) : ICommand<Result<bool>>;

public class DefineRolePolicyHandler(
    IIdentityService identityService,
    ICacheService cacheService               // 🌟 注入缓存服务
) : ICommandHandler<DefineRolePolicyCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DefineRolePolicyCommand request, CancellationToken cancellationToken)
    {
        // 1. 尝试在保安科创建角色名 (底层会瞬间落盘)
        var createResult = await identityService.CreateRoleAsync(request.RoleName);

        if (!createResult.IsSuccess)
        {
            return Result.Failure(createResult.Errors?.ToArray() ?? ["角色创建失败"]);
        }

        // ==========================================
        // 🌟 核心防线：补偿事务机制 (Compensating Transaction)
        // ==========================================
        try
        {
            // 2. 尝试向该角色写入具体的权限 Claims (底层也会瞬间落盘)
            var updateResult = await identityService.UpdateRolePermissionsAsync(request.RoleName, request.Permissions);

            if (!updateResult.IsSuccess || !updateResult.Value)
            {
                await RollbackRoleCreation(request.RoleName);
                return Result.Failure(updateResult.Errors?.ToArray() ?? ["角色权限分配失败，已撤销角色创建"]);
            }

            // 🌟 缓存双杀：角色定义变更后，爆破权限定义缓存
            await cacheService.RemoveAsync("iiot:permissions:v1:all-defined", cancellationToken);

            return Result.Success(true);
        }
        catch (Exception ex)
        {
            await RollbackRoleCreation(request.RoleName);
            return Result.Failure($"定义角色策略时发生异常，已执行回滚删除空壳角色。错误: {ex.Message}");
        }
    }

    private async Task RollbackRoleCreation(string roleName)
    {
        await identityService.RemoveRoleFromUserAsync(string.Empty, roleName);
    }
}
