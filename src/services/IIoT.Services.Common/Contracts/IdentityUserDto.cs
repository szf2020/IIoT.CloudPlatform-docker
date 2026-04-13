namespace IIoT.Services.Common.Contracts;

/// <summary>
/// 用户身份查询结果。
/// </summary>
public record IdentityUserDto(Guid Id, string EmployeeNo, IList<string> Roles);
