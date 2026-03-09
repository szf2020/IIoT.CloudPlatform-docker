namespace IIoT.HttpApi.Models;

// 注册请求
public record EmployeeRegisterRequest(string EmployeeNo, string RealName, string Password);

// 登录请求
public record UserLoginRequest(string EmployeeNo, string Password);

// 创建角色请求
public record CreateRoleRequest(string RoleName);