using IIoT.Services.Common.Contracts;
using IIoT.SharedKernel.Result;
using Microsoft.AspNetCore.Identity;

namespace IIoT.EntityFrameworkCore.Identity;

public sealed class AccountService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager) : IAccountService
{
    public async Task<Result> CreateUserAsync(Guid id, string employeeNo, string password)
    {
        var user = new ApplicationUser { Id = id, UserName = employeeNo };
        var result = await userManager.CreateAsync(user, password);
        return result.ToResult();
    }

    public async Task<Result> DeleteUserAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null) return Result.Failure("\u7528\u6237\u4E0D\u5B58\u5728");

        var result = await userManager.DeleteAsync(user);
        return result.ToResult();
    }

    public async Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null) return Result.Failure("\u7528\u6237\u4E0D\u5B58\u5728");

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        return result.ToResult();
    }

    public async Task<Result<bool>> ResetPasswordAsync(Guid userId, string newPassword)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null) return Result.Failure("\u7528\u6237\u4E0D\u5B58\u5728");

        var removeResult = await userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
            return Result.Failure(removeResult.Errors.Select(e => e.Description).ToArray());

        var addResult = await userManager.AddPasswordAsync(user, newPassword);
        if (!addResult.Succeeded)
            return Result.Failure(addResult.Errors.Select(e => e.Description).ToArray());

        return Result.Success(true);
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

    public async Task<Result<bool>> AssignRoleToUserAsync(string employeeNo, string roleName)
    {
        var user = await userManager.FindByNameAsync(employeeNo);
        if (user == null) return Result.Failure("\u7528\u6237\u4E0D\u5B58\u5728");

        if (!await roleManager.RoleExistsAsync(roleName)) return Result.Failure("\u89D2\u8272\u672A\u5B9A\u4E49");

        if (await userManager.IsInRoleAsync(user, roleName)) return Result.Success(true);

        var result = await userManager.AddToRoleAsync(user, roleName);
        return result.Succeeded
            ? Result.Success(true)
            : Result.Failure(result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<IList<string>> GetRolesAsync(string employeeNo)
    {
        var user = await userManager.FindByNameAsync(employeeNo);
        return user == null ? new List<string>() : await userManager.GetRolesAsync(user);
    }
}
