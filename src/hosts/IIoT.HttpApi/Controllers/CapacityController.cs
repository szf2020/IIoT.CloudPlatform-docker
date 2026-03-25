using IIoT.HttpApi.Infrastructure;
using IIoT.ProductionService.Commands.Capacities;
using Microsoft.AspNetCore.Mvc;

namespace IIoT.HttpApi.Controllers;

/// <summary>
/// 产能科：接收边缘端上报的每日产能汇总数据
/// </summary>
[Route("api/v1/[controller]")]
[ApiController]
[Tags("产能科 - 产能汇总接收")]
public class CapacityController : ApiControllerBase
{
    /// <summary>
    /// 接收单日产能汇总
    /// </summary>
    [HttpPost("daily")]
    public async Task<IActionResult> ReceiveDaily([FromBody] ReceiveDailyCapacityCommand command)
    {
        var result = await Sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }
}