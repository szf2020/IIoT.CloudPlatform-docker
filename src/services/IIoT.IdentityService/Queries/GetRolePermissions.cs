using IIoT.Services.Common.Attributes;
using IIoT.Services.Common.Contracts;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Result;

namespace IIoT.IdentityService.Queries;

/// <summary>
/// 角色权限详情 DTO：角色名 + 当前绑定的全部权限点
/// </summary>
public record RolePermissionsDto(
    string RoleName,
    List<string> Permissions
);

/// <summary>
/// 交互查询：获取指定角色当前绑定的所有权限点
/// </summary>
/// <remarks>
/// 前端角色权限编辑页需要先读出当前权限，再让管理员勾选。
/// </remarks>
[AuthorizeRequirement("Role.Define")]
public record GetRolePermissionsQuery(string RoleName) : IQuery<Result<RolePermissionsDto>>;

public class GetRolePermissionsHandler(
    IIdentityService identityService
) : IQueryHandler<GetRolePermissionsQuery, Result<RolePermissionsDto>>
{
    public async Task<Result<RolePermissionsDto>> Handle(GetRolePermissionsQuery request, CancellationToken cancellationToken)
    {
        // 底层 GetRolePermissionsAsync 在角色不存在时返回空列表 []，不会返回 null
        // 前端通过列表是否为空来判断角色是否配置了权限
        var permissions = await identityService.GetRolePermissionsAsync(request.RoleName);

        return Result.Success(new RolePermissionsDto(request.RoleName, permissions));
    }
}
