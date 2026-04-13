using IIoT.EmployeeService.Commands.Employees;
using IIoT.EmployeeService.Queries.Employees;
using IIoT.HttpApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace IIoT.HttpApi.Controllers;

/// <summary>
/// 员工档案与权限接口。
/// </summary>
[Route("api/v1/[controller]")]
[ApiController]
[Tags("人事科 - 组织与管辖权管理")]
public class EmployeeController : ApiControllerBase
{
    /// <summary>
    /// 获取员工分页列表。
    [HttpGet]
    public async Task<IActionResult> GetPagedList([FromQuery] GetEmployeePagedListQuery query)
    {
        var result = await Sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 获取员工详情。
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetail([FromRoute] Guid id)
    {
        var query = new GetEmployeeDetailQuery(id);
        var result = await Sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 获取员工访问范围。
    [HttpGet("{id}/access")]
    public async Task<IActionResult> GetAccess([FromRoute] Guid id)
    {
        var query = new GetEmployeeAccessQuery(id);
        var result = await Sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 员工入职建档。
    [HttpPost]
    public async Task<IActionResult> Onboard([FromBody] OnboardEmployeeCommand command)
    {
        var result = await Sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 更新员工基础资料。
    [HttpPut("{id}/profile")]
    public async Task<IActionResult> UpdateProfile([FromRoute] Guid id, [FromBody] UpdateEmployeeProfileCommand command)
    {
        // 使用路由 ID 覆盖命令体 ID。
        var result = await Sender.Send(command with { EmployeeId = id });
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }
    /// <summary>
    /// 更新员工访问范围。
    [HttpPut("{id}/access")]
    public async Task<IActionResult> UpdateAccess([FromRoute] Guid id, [FromBody] UpdateEmployeeAccessCommand command)
    {
        var result = await Sender.Send(command with { EmployeeId = id });
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 停用员工。
    [HttpPut("{id}/deactivate")]
    public async Task<IActionResult> Deactivate([FromRoute] Guid id)
    {
        var command = new DeactivateEmployeeCommand(id);
        var result = await Sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 删除员工档案。
    [HttpDelete("{id}")]
    public async Task<IActionResult> Terminate([FromRoute] Guid id)
    {
        var command = new TerminateEmployeeCommand(id);
        var result = await Sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }
}
