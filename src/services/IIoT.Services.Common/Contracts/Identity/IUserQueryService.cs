namespace IIoT.Services.Common.Contracts.Identity;

public interface IUserQueryService
{
    Task<IList<IdentityAccountDto>> GetAllUsersAsync();

    Task<IdentityAccountDto?> GetUserByEmployeeNoAsync(string employeeNo);
}
