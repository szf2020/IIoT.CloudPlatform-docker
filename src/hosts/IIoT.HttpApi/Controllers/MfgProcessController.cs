using IIoT.EmployeeService.Commands;
using IIoT.EmployeeService.Commands.MfgProcesses;
using IIoT.EmployeeService.Queries;
using IIoT.EmployeeService.Queries.MfgProcesses;
using IIoT.HttpApi.Infrastructure;
using IIoT.SharedKernel.Paging;
using Microsoft.AspNetCore.Mvc;

namespace IIoT.HttpApi.Controllers;

/// <summary>
/// 工序科：制造工序管理中枢 (负责工序的定义、维护与全量查询)
/// </summary>
[Route("api/v1/[controller]")]
[ApiController]
[Tags("工序科 - 制造工序管理")]
public class MfgProcessController : ApiControllerBase
{
    /// <summary>
    /// 获取工序分页列表 (带搜索)
    /// </summary>
    /// <param name="pagination">分页参数</param>
    /// <param name="keyword">搜索关键字</param>
    [HttpGet]
    public async Task<IActionResult> GetPagedList([FromQuery] Pagination pagination, [FromQuery] string? keyword = null)
    {
        pagination ??= new Pagination();
        var query = new GetMfgProcessPagedListQuery(pagination, keyword);
        var result = await Sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 获取全量工序列表 (供下拉选择器使用，无分页)
    /// </summary>
    /// <remarks>
    /// 工序数量在工厂场景下通常不超过百条，直接全量返回。
    /// 设备注册、配方创建、员工管辖权分配等场景均依赖此接口。
    /// </remarks>
    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var result = await Sender.Send(new GetAllMfgProcessesQuery());
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 创建新的制造工序
    /// </summary>
    /// <param name="command">工序定义 (编码 + 名称)</param>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMfgProcessCommand command)
    {
        var result = await Sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 更新工序基础档案
    /// </summary>
    /// <param name="id">工序 ID</param>
    /// <param name="command">需要更新的资料</param>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateMfgProcessCommand command)
    {
        var result = await Sender.Send(command with { ProcessId = id });
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 删除工序 (含关联数据安全校验)
    /// </summary>
    /// <remarks>
    /// 若工序下仍有设备或配方挂载，删除将被拒绝。
    /// </remarks>
    /// <param name="id">工序 ID</param>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        var command = new DeleteMfgProcessCommand(id);
        var result = await Sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }
}