using System.Net;
using FluentAssertions;
using IIoT.EndToEndTests.ApiClients;
using Xunit;

namespace IIoT.EndToEndTests;

public class EmployeeOnboardingTests : IAsyncLifetime
{
    private readonly IIoTAppFixture _fixture = new();

    public Task InitializeAsync() => _fixture.StartAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync().AsTask();

    [Fact]
    public async Task 验证从入职到权限拦截的完整业务闭环()
    {
        var identityApi = _fixture.GetApi<IIdentityApi>();
        var employeeApi = _fixture.GetApi<IEmployeeApi>();
        var recipeApi = _fixture.GetApi<IRecipeApi>();

        // 1. Admin 登录
        var adminLogin = await identityApi.LoginAsync(new LoginRequest("101650", "Ljh123456!"));
        if (!adminLogin.IsSuccessStatusCode)
            throw new Exception($"Admin登录失败: {adminLogin.Error?.Content}");
        _fixture.SetAuthToken(adminLogin.Content!);

        // 2. 定义角色策略 (🌟 修复：一步到位传 RoleName 和 Permissions)
        var roleName = "Tech_" + Guid.NewGuid().ToString("N")[..6];
        var roleRes = await identityApi.DefineRolePolicyAsync(new DefineRoleRequest(roleName, ["Recipe.Read"]));

        if (!roleRes.IsSuccessStatusCode)
        {
            var errorMsg = roleRes.Error?.Content ?? "无具体错误信息";
            throw new Exception($"\n🚨 【角色定义失败】\n状态码: {roleRes.StatusCode}\n报错: {errorMsg}\n");
        }

        // 3. 员工入职
        var newEmpNo = "E" + Guid.NewGuid().ToString("N")[..5];
        var onboardRes = await employeeApi.OnboardAsync(new OnboardRequest(
            EmployeeNo: newEmpNo,
            RealName: "测试工",
            Password: "User123!",
            RoleName: roleName));

        if (!onboardRes.IsSuccessStatusCode)
        {
            var errorMsg = onboardRes.Error?.Content ?? "无具体错误信息";
            throw new Exception($"\n🚨 【入职接口崩溃】\n状态码: {onboardRes.StatusCode}\n报错: {errorMsg}\n");
        }

        // 4. 新员工登录
        var userLogin = await identityApi.LoginAsync(new LoginRequest(newEmpNo, "User123!"));
        if (!userLogin.IsSuccessStatusCode)
            throw new Exception($"新员工登录失败: {userLogin.Error?.Content}");
        _fixture.SetAuthToken(userLogin.Content!);

        // 5. 权限验证
        var readRes = await recipeApi.GetRecipesAsync();
        readRes.IsSuccessStatusCode.Should().BeTrue("拥有读取权限，应返回 200 OK");

        var createRes = await recipeApi.CreateRecipeAsync(new { Name = "非法配方" });
        createRes.StatusCode.Should().Be(HttpStatusCode.Forbidden, "无写入权限，必须拦截并返回 403 Forbidden");
    }
}