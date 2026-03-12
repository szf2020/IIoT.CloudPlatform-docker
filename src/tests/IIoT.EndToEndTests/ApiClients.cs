// 文件位置: src/tests/IIoT.EndToEndTests/ApiClients.cs

using Refit;

namespace IIoT.EndToEndTests.ApiClients;

public record LoginRequest(string EmployeeNo, string Password);
public record DefineRoleRequest(string RoleName, string[] Permissions);
public record OnboardRequest(
    string EmployeeNo,
    string RealName,
    string Password,
    string? RoleName = null,
    List<Guid>? ProcessIds = null,
    List<Guid>? DeviceIds = null);

public interface IIdentityApi
{
    [Post("/api/v1/identity/login")]
    Task<ApiResponse<string>> LoginAsync([Body] LoginRequest request);

    [Post("/api/v1/identity/roles")]
    Task<IApiResponse> DefineRolePolicyAsync([Body] DefineRoleRequest request);
}

public interface IEmployeeApi
{
    [Post("/api/v1/employee")]
    Task<IApiResponse> OnboardAsync([Body] OnboardRequest request);

    // 🌟 新增：用于测试准备阶段创建真实的工序
    [Post("/api/v1/employee/processes")]
    Task<ApiResponse<Guid>> CreateProcessAsync([Body] object process);
}

public interface IRecipeApi
{
    // 🌟 核心修复：必须指定泛型 ApiResponse<string>，Refit 才会帮你把业务数据作为字符串保留下来
    [Get("/api/v1/recipe")]
    Task<ApiResponse<string>> GetRecipesAsync();

    [Get("/api/v1/recipe/{id}")]
    Task<IApiResponse> GetRecipeByIdAsync(Guid id);

    [Post("/api/v1/recipe")]
    Task<IApiResponse> CreateRecipeAsync([Body] object recipe);
}