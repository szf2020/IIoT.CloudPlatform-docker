namespace IIoT.Services.Common.Contracts;

/// <summary>
/// 用户只读查询服务。
/// </summary>
public interface IUserQueryService
{
    Task<IList<IdentityUserDto>> GetAllUsersAsync();

    Task<IdentityUserDto?> GetUserByEmployeeNoAsync(string employeeNo);
}
