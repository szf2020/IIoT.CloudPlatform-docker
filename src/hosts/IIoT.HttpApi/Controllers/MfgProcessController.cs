using IIoT.EmployeeService.Commands;
using IIoT.EmployeeService.Commands.MfgProcesses;
using IIoT.EmployeeService.Queries;
using IIoT.EmployeeService.Queries.MfgProcesses;
using IIoT.HttpApi.Infrastructure;
using IIoT.SharedKernel.Paging;
using Microsoft.AspNetCore.Mvc;

namespace IIoT.HttpApi.Controllers;

/// <summary>
/// 工序接口。
/// </summary>
[Route("api/v1/[controller]")]
[ApiController]
[Tags("工序科 - 制造工序管理")]
public class MfgProcessController : ApiControllerBase
{
    /// <summary>
    /// 获取工序分页列表。
    [HttpGet]
    public async Task<IActionResult> GetPagedList([FromQuery] Pagination pagination, [FromQuery] string? keyword = null)
    {
        pagination ??= new Pagination();
        var query = new GetMfgProcessPagedListQuery(pagination, keyword);
        var result = await Sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 获取全量工序列表。
    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var result = await Sender.Send(new GetAllMfgProcessesQuery());
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 创建工序。
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMfgProcessCommand command)
    {
        var result = await Sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 更新工序基础资料。
    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateMfgProcessCommand command)
    {
        var result = await Sender.Send(command with { ProcessId = id });
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 删除工序。
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        var command = new DeleteMfgProcessCommand(id);
        var result = await Sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }
}
