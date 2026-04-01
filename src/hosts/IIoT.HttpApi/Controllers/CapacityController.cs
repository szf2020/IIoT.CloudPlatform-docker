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
    /// 接收半小时槽位产能汇总（新版）
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
    /// 半小时槽位产能分页查询（缓存优先，带设备名称和良率）
    /// </summary>
    [HttpGet("hourly")]
    public async Task<IActionResult> GetHourlyPaged(
        [FromQuery] Pagination pagination,
        [FromQuery] DateOnly? date = null,
        [FromQuery] Guid? deviceId = null)
    {
        var query = new GetHourlyCapacityPagedQuery(pagination, date, deviceId);
        var result = await Sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 单机台半小时槽位产能汇总查询（缓存优先，所有日期范围均有效）
    /// </summary>
    [HttpGet("hourly/device/{deviceId}")]
    public async Task<IActionResult> GetHourlySummaryByDevice(
        [FromRoute] Guid deviceId,
        [FromQuery] DateOnly? startDate = null,
        [FromQuery] DateOnly? endDate = null)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var query = new GetHourlyCapacityByDeviceQuery(
            deviceId,
            startDate ?? today.AddMonths(-1),
            endDate ?? today);

        var result = await Sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }
}