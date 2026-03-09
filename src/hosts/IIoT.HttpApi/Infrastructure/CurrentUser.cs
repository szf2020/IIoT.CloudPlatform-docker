using System.Security.Claims;
using IIoT.Services.Common.Contracts; // 假设你的 ICurrentUser 接口在这里

namespace IIoT.HttpApi.Infrastructure;

public class CurrentUser : ICurrentUser
{
    public string? Id { get; }
    public string? UserName { get; }
    public string? Role { get; }
    public bool IsAuthenticated { get; }

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;

        if (user == null) return;

        if (!user.Identity!.IsAuthenticated) return;

        // 这里的 ClaimTypes 会自动匹配 JWT 里的标准字段
        Id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        UserName = user.FindFirstValue(ClaimTypes.Name);
        Role = user.FindFirstValue(ClaimTypes.Role);

        IsAuthenticated = true;
    }
}