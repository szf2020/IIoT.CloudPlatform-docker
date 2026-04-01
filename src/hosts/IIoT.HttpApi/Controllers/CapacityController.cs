using IIoT.HttpApi.Infrastructure;
using IIoT.ProductionService.Commands.Capacities;
using IIoT.ProductionService.Queries.Capacities;
using IIoT.SharedKernel.Paging;
using Microsoft.AspNetCore.Mvc;

namespace IIoT.HttpApi.Controllers;

/// <summary>
/// 产能科：接收边缘端产能汇总 + 云端后台产能查询
/// </summary>
[Route("api/v1/[controller]")]
[ApiController]
[Tags("产能科 - 产能汇总接收与查询")]
public class CapacityController : ApiControllerBase
{
    // ==========================================
    // 写入接口（边缘端上报）
    // ==========================================

    /// <summary>
    /// 接收半小时产能汇总（用于边缘端补传）
    /// </summary>
    [HttpPost("hourly")]
    public async Task<IActionResult> ReceiveHourly([FromBody] ReceiveHourlyCapacityCommand command)
    {
        var result = await Sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    // ==========================================
    // 查询接口（云端后台）
    // ==========================================

    /// <summary>
    /// 所有机台产能分页查询（延迟加载，带设备名称和良率，缓存优先）
    /// </summary>
    [HttpGet("daily")]
    public async Task<IActionResult> GetDailyPaged(
        [FromQuery] Pagination pagination,
        [FromQuery] DateOnly? date = null,
        [FromQuery] Guid? deviceId = null)
    {
        var query = new GetDailyCapacityPagedQuery(pagination, date, deviceId);
        var result = await Sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }
}