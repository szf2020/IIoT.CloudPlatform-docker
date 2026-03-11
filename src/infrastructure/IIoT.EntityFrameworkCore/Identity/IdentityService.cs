using IIoT.Services.Common.Contracts;
using IIoT.SharedKernel.Result;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IIoT.EntityFrameworkCore.Identity;

public class IdentityService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IIoTDbContext dbContext) : IIdentityService
{
    private const string PermissionClaimType = "Permission";

    #region 1. 账号管理 (Account Management)

    public async Task<Result> CreateUserAsync(Guid id, string employeeNo, string password)
    {
        var user = new ApplicationUser { Id = id, UserName = employeeNo };
        var result = await userManager.CreateAsync(user, password);
        return ToResult(result);
    }

    public async Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null) return Result.Failure("用户不存在");

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        return ToResult(result);
    }

    public async Task<Result> DeleteUserAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null) return Result.Failure("用户不存在");

        var result = await userManager.DeleteAsync(user);
        return ToResult(result);
    }

    public async Task<Result<bool>> CheckPasswordAsync(string employeeNo, string password)
    {
        var user = await userManager.FindByNameAsync(employeeNo);
        if (user == null) return Result.Success(false);

        var isValid = await userManager.CheckPasswordAsync(user, password);
        return Result.Success(isValid);
    }

    public async Task<Guid?> GetUserIdByEmployeeNoAsync(string employeeNo)
    {
        var user = await userManager.FindByNameAsync(employeeNo);
        return user?.Id;
    }

    #endregion 1. 账号管理 (Account Management)

    #region 2. 查询服务 (Query Services)

    public async Task<IList<IdentityUserDto>> GetAllUsersAsync()
    {
        var users = await userManager.Users.AsNoTracking().ToListAsync();
        var userDtos = new List<IdentityUserDto>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            userDtos.Add(new IdentityUserDto(user.Id, user.UserName!, roles));
        }

        return userDtos;
    }

    public async Task<IdentityUserDto?> GetUserByEmployeeNoAsync(string employeeNo)
    {
        var user = await userManager.FindByNameAsync(employeeNo);
        if (user == null) return null;

        var roles = await userManager.GetRolesAsync(user);
        return new IdentityUserDto(user.Id, user.UserName!, roles);
    }

    public async Task<IList<string>> GetAllRolesAsync()
    {
        return await roleManager.Roles.Select(r => r.Name!).ToListAsync();
    }

    public async Task<IList<string>> GetRolesAsync(string employeeNo)
    {
        var user = await userManager.FindByNameAsync(employeeNo);
        return user == null ? new List<string>() : await userManager.GetRolesAsync(user);
    }

    #endregion 2. 查询服务 (Query Services)

    #region 3. 角色与权限 (Roles & Permissions)

    public async Task<Result> CreateRoleAsync(string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName)) return Result.Success();

        var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
        return ToResult(result);
    }

    public async Task<Result<bool>> UpdateRolePermissionsAsync(string roleName, List<string> permissions)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role == null) return Result.Failure("角色不存在"); // 🌟 修复：直接使用 Result.Failure，利用隐式转换

        var claims = await roleManager.GetClaimsAsync(role);
        var existingPermissions = claims.Where(c => c.Type == PermissionClaimType).ToList();

        foreach (var claim in existingPermissions.Where(c => !permissions.Contains(c.Value)))
            await roleManager.RemoveClaimAsync(role, claim);

        foreach (var p in permissions.Where(p => !existingPermissions.Any(c => c.Value == p)))
            await roleManager.AddClaimAsync(role, new Claim(PermissionClaimType, p));

        return Result.Success(true);
    }

    public async Task<Result<bool>> UpdateUserPersonalPermissionsAsync(Guid userId, List<string> permissions)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null) return Result.Failure("用户不存在"); // 🌟 修复：直接使用 Result.Failure

        var claims = await userManager.GetClaimsAsync(user);
        var existingPermissions = claims.Where(c => c.Type == PermissionClaimType).ToList();

        foreach (var claim in existingPermissions.Where(c => !permissions.Contains(c.Value)))
            await userManager.RemoveClaimAsync(user, claim);

        foreach (var p in permissions.Where(p => !existingPermissions.Any(c => c.Value == p)))
            await userManager.AddClaimAsync(user, new Claim(PermissionClaimType, p));

        return Result.Success(true);
    }

    #endregion 3. 角色与权限 (Roles & Permissions)

    #region 4. 分配管理 (Assignment Management)

    public async Task<Result<bool>> AssignRoleToUserAsync(string employeeNo, string roleName)
    {
        var user = await userManager.FindByNameAsync(employeeNo);
        if (user == null) return Result.Failure("用户不存在"); // 🌟 修复：直接使用 Result.Failure

        if (!await roleManager.RoleExistsAsync(roleName)) return Result.Failure("角色未定义");

        if (await userManager.IsInRoleAsync(user, roleName)) return Result.Success(true);

        var result = await userManager.AddToRoleAsync(user, roleName);
        // 🌟 修复：IdentityResult -> Result -> 隐式转换 Result<bool>
        return result.Succeeded ? Result.Success(true) : Result.Failure(result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<Result> RemoveRoleFromUserAsync(string employeeNo, string roleName)
    {
        var user = await userManager.FindByNameAsync(employeeNo);
        if (user == null) return Result.Failure("用户不存在");

        var result = await userManager.RemoveFromRoleAsync(user, roleName);
        return ToResult(result);
    }

    #endregion 4. 分配管理 (Assignment Management)

    private static Result ToResult(IdentityResult result)
    {
        return result.Succeeded
            ? Result.Success()
            : Result.Failure(result.Errors.Select(e => e.Description).ToArray());
    }
}