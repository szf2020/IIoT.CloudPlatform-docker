using System.Security.Claims;
using IIoT.HttpApi.Infrastructure;
using IIoT.HttpApi.Models;
using IIoT.IdentityService.Commands;
using Microsoft.AspNetCore.Mvc;

namespace IIoT.HttpApi.Controllers;

[Route("/api/identity")]
public class IdentityController : ApiControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(EmployeeRegisterRequest request)
    {
        // 🌟 将前端的 Request DTO 映射为后端的 Command
        var result = await Sender.Send(new CreateEmployeeCommand(request.EmployeeNo, request.RealName, request.Password));
        return ReturnResult(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(UserLoginRequest request)
    {
        var result = await Sender.Send(new LoginUserCommand(request.EmployeeNo, request.Password));
        return ReturnResult(result);
    }

    [HttpPost("role")]
    public async Task<IActionResult> CreateRole(CreateRoleRequest request)
    {
        var result = await Sender.Send(new CreateRoleCommand(request.RoleName));
        return ReturnResult(result);
    }

    [HttpPost("test")]
    public IActionResult Test()
    {
        return Ok(new
        {
            IsAuthenticated = User.Identity?.IsAuthenticated,
            // 🌟 之前在 JwtTokenGenerator 里，我们把 JwtRegisteredClaimNames.UniqueName 设为了工号
            EmployeeNo = User.FindFirstValue(ClaimTypes.Name)
        });
    }
}