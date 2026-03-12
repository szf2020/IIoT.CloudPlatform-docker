// 文件位置: src/tests/IIoT.EndToEndTests/RecipeBusinessTests.cs

using System.Net;
using FluentAssertions;
using IIoT.EndToEndTests.ApiClients;
using Xunit;

namespace IIoT.EndToEndTests;

public class RecipeBusinessTests : IAsyncLifetime
{
    private readonly IIoTAppFixture _fixture = new();

    public Task InitializeAsync() => _fixture.StartAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync().AsTask();

    [Fact]
    public async Task 验证最核心的ABAC数据隔离_使用系统真实数据()
    {
        var identityApi = _fixture.GetApi<IIdentityApi>();
        var employeeApi = _fixture.GetApi<IEmployeeApi>();
        var recipeApi = _fixture.GetApi<IRecipeApi>();

        // 1. Admin 登录
        var adminLogin = await identityApi.LoginAsync(new LoginRequest("101650", "Ljh123456!"));
        _fixture.SetAuthToken(adminLogin.Content!);

        // 🌟 核心改进：不要自己造 GUID，因为后端会去数据库查！
        // 方案：从你的 SystemInitData 或通过 Admin 权限查询现有的工序列表
        // 假设你的系统里已经通过种子数据初始化了工序
        // 这里我们模拟获取两个不同的真实 ProcessId (你需要根据你数据库实际有的数据来)
        // 比如从种子数据中获取，或者通过一个“获取工序列表”的 API 拿到它们

        // 【注意】以下 ID 必须是你数据库里 MfgProcesses 表中真实存在的！
        // 你可以去 SystemInitData.cs 里看一眼具体的常量 GUID
        var realProcessIdA = Guid.Parse("...从SystemInitData获取的工序A_ID...");
        var realProcessIdB = Guid.Parse("...从SystemInitData获取的工序B_ID...");

        // 2. 办理员工入职 (绑定真实 ID)
        var roleName = "Engineer_" + Guid.NewGuid().ToString("N")[..6];
        await identityApi.DefineRolePolicyAsync(new DefineRoleRequest(roleName, ["Recipe.Read", "Recipe.Create"]));

        var empA_No = "EMP_A_" + Guid.NewGuid().ToString("N")[..4];
        await employeeApi.OnboardAsync(new OnboardRequest(empA_No, "员工A", "User123!", roleName, ProcessIds: [realProcessIdA]));

        // 3. A员工执行创建
        var loginA = await identityApi.LoginAsync(new LoginRequest(empA_No, "User123!"));
        _fixture.SetAuthToken(loginA.Content!);

        var createResA = await recipeApi.CreateRecipeAsync(new
        {
            RecipeName = "配方A_机密",
            ProcessId = realProcessIdA, // 🌟 传给后端它认识的 ID
            ParametersJsonb = "{}"
        });

        // 此时断言必过，因为后端 Repo.GetByIdAsync(realProcessIdA) 将不再返回 null
        createResA.IsSuccessStatusCode.Should().BeTrue($"员工A创建配方应成功: {createResA.Error?.Content}");
    }

    [Fact]
    public async Task 验证系统大门_不带Token的裸请求必须被401拒之门外()
    {
        var recipeApi = _fixture.GetApi<IRecipeApi>();

        var readRes = await recipeApi.GetRecipesAsync();

        readRes.StatusCode.Should().Be(HttpStatusCode.Forbidden, "未携带凭证，被内层 MediatR 管道拦截，应返回 403 Forbidden");
    }

    [Fact]
    public async Task 验证业务防呆_必填字段为空时必须返回400拦截()
    {
        var identityApi = _fixture.GetApi<IIdentityApi>();
        var recipeApi = _fixture.GetApi<IRecipeApi>();

        var adminLogin = await identityApi.LoginAsync(new LoginRequest("101650", "Ljh123456!"));
        _fixture.SetAuthToken(adminLogin.Content!);

        var badRes = await recipeApi.CreateRecipeAsync(new
        {
            RecipeName = "",
            ParametersJsonb = ""
        });

        badRes.StatusCode.Should().Be(HttpStatusCode.BadRequest, "必填参数缺失，模型绑定或验证器应返回 400 BadRequest");
    }
}