using IIoT.HttpApi.Infrastructure;
using IIoT.IdentityService.Commands;
using IIoT.IdentityService.Queries;
using Microsoft.AspNetCore.Mvc;

namespace IIoT.HttpApi.Controllers;

/// <summary>
/// 保安科：身份与安全认证中枢 (负责登录、发牌、底层账号与权限点分配)
/// </summary>
[Route("api/v1/[controller]")]
[ApiController]
[Tags("保安科 - 身份与安全认证")]
public class IdentityController : ApiControllerBase
{
    /// <summary>
    /// 极速登录并获取 JWT 令牌
    /// </summary>
    /// <param name="command">包含用户名和密码的登录凭证</param>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand command)
    {
        var result = await Sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 注册底层系统账号 (建立 SharedId)
    /// </summary>
    /// <param name="command">底层账号注册信息</param>
    [HttpPost("register")]
    public async Task<IActionResult> RegisterAccount([FromBody] RegisterAccountCommand command)
    {
        var result = await Sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 修改当前登录账号的密码
    /// </summary>
    /// <param name="command">包含旧密码与新密码的指令</param>
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
    {
        var result = await Sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 物理销毁底层账号 (将触发人事档案的级联删除)
    /// </summary>
    /// <param name="id">要销毁的底层账号 ID (SharedId)</param>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAccount([FromRoute] Guid id)
    {
        var command = new DeleteAccountCommand(id);
        var result = await Sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 获取全系统底层角色策略列表
    /// </summary>
    [HttpGet("roles")]
    public async Task<IActionResult> GetAllRoles()
    {
        var result = await Sender.Send(new GetAllRolesQuery());
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 定义全新的系统角色
    /// </summary>
    /// <param name="command">角色定义指令</param>
    [HttpPost("roles")]
    public async Task<IActionResult> DefineRolePolicy([FromBody] DefineRolePolicyCommand command)
    {
        var result = await Sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 更新指定角色的行为权限点 (Claims 组)
    /// </summary>
    /// <param name="roleName">目标角色名 (如: Operator)</param>
    /// <param name="permissions">最新配置的行为权限点字符串集合</param>
    [HttpPut("roles/{roleName}/permissions")]
    public async Task<IActionResult> UpdateRolePermissions(
        [FromRoute] string roleName,
        [FromBody] List<string> permissions)
    {
        var command = new UpdateRolePermissionsCommand(roleName, permissions);
        var result = await Sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }
}