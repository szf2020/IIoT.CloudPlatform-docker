using IIoT.HttpApi.Infrastructure;
using IIoT.ProductionService.Commands.PassStations;
using Microsoft.AspNetCore.Mvc;

namespace IIoT.HttpApi.Controllers;

/// <summary>
/// 过站数据科：接收边缘端上报的电芯过站检测数据
/// </summary>
[Route("api/v1/[controller]")]
[ApiController]
[Tags("过站数据科 - 生产数据接收")]
public class PassStationController : ApiControllerBase
{
    /// <summary>
    /// 接收单条注液工序过站数据
    /// </summary>
    [HttpPost("injection")]
    public async Task<IActionResult> ReceiveInjection([FromBody] ReceiveInjectionPassCommand command)
    {
        var result = await Sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 批量接收注液工序过站数据（边缘端补传用）
    /// </summary>
    [HttpPost("injection/batch")]
    public async Task<IActionResult> ReceiveInjectionBatch([FromBody] List<ReceiveInjectionPassCommand> commands)
    {
        foreach (var command in commands)
        {
            var result = await Sender.Send(command);
            if (!result.IsSuccess) return BadRequest(result.Errors);
        }

        return Ok(new { message = $"已接收 {commands.Count} 条注液过站数据" });
    }
}