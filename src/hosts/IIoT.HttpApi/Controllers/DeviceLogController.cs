using IIoT.HttpApi.Infrastructure;
using IIoT.ProductionService.Commands.DeviceLogs;
using Microsoft.AspNetCore.Mvc;

namespace IIoT.HttpApi.Controllers;

/// <summary>
/// 日志科：接收边缘端推送的设备运行日志
/// </summary>
[Route("api/v1/[controller]")]
[ApiController]
[Tags("日志科 - 设备日志接收")]
public class DeviceLogController : ApiControllerBase
{
    /// <summary>
    /// 接收设备日志（支持批量）
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Receive([FromBody] ReceiveDeviceLogCommand command)
    {
        var result = await Sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }
}