using IIoT.Services.Common.Attributes;
using IIoT.Services.Common.Contracts;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Result;

namespace IIoT.IdentityService.Queries;

/// <summary>
/// 权限点分组 DTO：按模块分组展示
/// </summary>
public record PermissionGroupDto(
    string GroupName,
    List<string> Permissions
);

/// <summary>
/// 交互查询：获取系统全部已定义的权限点 (动态聚合 + 内置清单兜底)
/// </summary>
/// <remarks>
/// 前端角色编辑页动态拉取此接口，不再依赖硬编码的 permissions.ts。
/// 带 Redis 缓存抗压，角色权限变更时由 Command 端负责缓存双杀。
/// </remarks>
[AuthorizeRequirement("Role.Define")]
public record GetAllDefinedPermissionsQuery() : IQuery<Result<List<PermissionGroupDto>>>;

public class GetAllDefinedPermissionsHandler(
    IIdentityService identityService,
    ICacheService cacheService
) : IQueryHandler<GetAllDefinedPermissionsQuery, Result<List<PermissionGroupDto>>>
{
    private const string CacheKey = "iiot:permissions:v1:all-defined";

    /// <summary>
    /// 系统内置权限点清单 (兜底：即使没有角色绑定过，这些权限也会出现在前端勾选器中)
    /// 新增后端 [AuthorizeRequirement] 时，在此处同步追加即可，前端自动展示。
    /// </summary>
    private static readonly List<string> BuiltInPermissions =
    [
        // 员工
        "Employee.Read", "Employee.Onboard", "Employee.Update",
        "Employee.UpdateAccess", "Employee.Deactivate", "Employee.Terminate",
        // 工序
        "Process.Read", "Process.Create", "Process.Update", "Process.Delete",
        // 设备
        "Device.Read", "Device.Create", "Device.Update", "Device.Deactivate",
        // 配方
        "Recipe.Read", "Recipe.Create", "Recipe.Update",
        // 角色
        "Role.Define", "Role.Update",
    ];

    public async Task<Result<List<PermissionGroupDto>>> Handle(GetAllDefinedPermissionsQuery request, CancellationToken cancellationToken)
    {
        // ==========================================
        // 🌟 读链路：缓存绝对优先 (Cache-Aside 模式)
        // ==========================================
        var cached = await cacheService.GetAsync<List<PermissionGroupDto>>(CacheKey, cancellationToken);
        if (cached != null) return Result.Success(cached);

        // 1. 从所有角色中聚合已绑定的权限点
        var allRoles = await identityService.GetAllRolesAsync();
        var discoveredPermissions = new HashSet<string>();

        foreach (var roleName in allRoles)
        {
            var perms = await identityService.GetRolePermissionsAsync(roleName);
            foreach (var p in perms) discoveredPermissions.Add(p);
        }

        // 2. 合并内置清单
        foreach (var p in BuiltInPermissions) discoveredPermissions.Add(p);

        // 3. 按 "." 前缀分组 (如 Employee.Read → Employee 组)
        var grouped = discoveredPermissions
            .OrderBy(p => p)
            .GroupBy(p => p.Contains('.') ? p.Split('.')[0] : "Other")
            .Select(g => new PermissionGroupDto(g.Key, g.ToList()))
            .OrderBy(g => g.GroupName)
            .ToList();

        // 回写缓存 (权限定义变动极少，4 小时过期)
        await cacheService.SetAsync(CacheKey, grouped, TimeSpan.FromHours(4), cancellationToken);

        return Result.Success(grouped);
    }
}
