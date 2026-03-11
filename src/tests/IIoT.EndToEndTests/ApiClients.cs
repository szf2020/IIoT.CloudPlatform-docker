using Refit;

namespace IIoT.EndToEndTests.ApiClients;

// --- DTO 定义 ---
public record LoginRequest(string EmployeeNo, string Password);

// 🌟 修复：严格对齐后端的 DefineRolePolicyCommand，必须带上 Permissions 数组
public record DefineRoleRequest(string RoleName, string[] Permissions);

public record OnboardRequest(
    string EmployeeNo,
    string RealName,
    string Password,
    string? RoleName = null,
    List<Guid>? ProcessIds = null,
    List<Guid>? DeviceIds = null);

// --- 接口定义 ---

public interface IIdentityApi
{
    [Post("/api/v1/identity/login")]
    Task<ApiResponse<string>> LoginAsync([Body] LoginRequest request);

    // 🌟 修复：恢复成一步到位的角色策略定义
    [Post("/api/v1/identity/roles")]
    Task<IApiResponse> DefineRolePolicyAsync([Body] DefineRoleRequest request);
}

public interface IEmployeeApi
{
    [Post("/api/v1/employee/onboard")]
    Task<IApiResponse> OnboardAsync([Body] OnboardRequest request);
}

public interface IRecipeApi
{
    [Get("/api/v1/recipe")]
    Task<IApiResponse> GetRecipesAsync();

    [Post("/api/v1/recipe")]
    Task<IApiResponse> CreateRecipeAsync([Body] object recipe);
}