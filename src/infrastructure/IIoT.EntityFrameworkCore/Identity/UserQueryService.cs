using IIoT.Services.Common.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IIoT.EntityFrameworkCore.Identity;

public sealed class UserQueryService(UserManager<ApplicationUser> userManager) : IUserQueryService
{
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
}
