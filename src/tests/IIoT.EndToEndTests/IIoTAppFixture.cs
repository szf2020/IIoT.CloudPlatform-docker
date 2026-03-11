using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Refit;
using System.Net.Http.Headers;

namespace IIoT.EndToEndTests;

public class IIoTAppFixture : IAsyncDisposable
{
    private DistributedApplication? _app;
    private HttpClient? _httpClient;

    public HttpClient HttpClient => _httpClient ?? throw new InvalidOperationException("环境未启动");

    public async Task StartAsync()
    {
        // 1. 启动 Aspire 编排
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.IIoT_AppHost>();
        _app = await builder.BuildAsync();
        await _app.StartAsync();

        // 2. 获取 HttpClient (确保名称与 AppHost.cs 一致)
        _httpClient = _app.CreateHttpClient("iiot-httpapi");

        // 3. 探针：循环探测直到 API 真正可用
        var ready = false;
        for (var i = 0; i < 30; i++)
        {
            try
            {
                // 探测登录接口，只要不报连接异常就说明服务器起来了
                using var res = await _httpClient.GetAsync("/api/v1/identity/login");
                ready = true;
                break;
            }
            catch { await Task.Delay(1000); }
        }
        if (!ready) throw new Exception("E2E 测试环境启动超时，请检查 Docker。");
    }

    // 生成 Refit 客户端
    public T GetApi<T>() => RestService.For<T>(HttpClient);

    // 注入 JWT Token
    public void SetAuthToken(string token)
    {
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async ValueTask DisposeAsync()
    {
        _httpClient?.Dispose();
        if (_app is not null) await _app.DisposeAsync();
    }
}