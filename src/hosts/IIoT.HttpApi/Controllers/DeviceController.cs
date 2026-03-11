using IIoT.HttpApi.Infrastructure;
using IIoT.ProductionService.Commands.Devices;
using IIoT.ProductionService.Queries.Devices;
using Microsoft.AspNetCore.Mvc;

namespace IIoT.HttpApi.Controllers;

/// <summary>
/// 设备科：物理机台与终端中枢 (负责机台寻址、注册与管辖列表维护)
/// </summary>
[Route("api/v1/[controller]")]
[ApiController]
[Tags("设备科 - 物理终端与寻址")]
public class DeviceController : ApiControllerBase
{
    /// <summary>
    /// 开机极速寻址 (机台向云端自证身份)
    /// </summary>
    /// <remarks>
    /// 物理设备开机时调用。通过自身网卡的 MAC 地址，换取系统分配的 UUID 与归属工序。
    /// </remarks>
    /// <param name="macAddress">物理 MAC 地址 (如: 00-1A-2B-3C-4D-5E)</param>
    [HttpGet("mac/{macAddress}")]
    public async Task<IActionResult> GetByMac([FromRoute] string macAddress)
    {
        var query = new GetDeviceByMacQuery(macAddress);
        var result = await Sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 获取当前登录人管辖范围内的设备分页列表
    /// </summary>
    /// <param name="query">分页与搜索参数</param>
    [HttpGet]
    public async Task<IActionResult> GetPagedList([FromQuery] GetMyDevicesPagedQuery query)
    {
        var result = await Sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 注册全新物理设备终端
    /// </summary>
    /// <param name="command">设备注册档案 (包含核心防伪 MAC 地址及归属工序)</param>
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterDeviceCommand command)
    {
        var result = await Sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 更新设备的业务基础档案 (设备名称、系统编号)
    /// </summary>
    /// <param name="id">设备 ID</param>
    /// <param name="command">需要更新的资料</param>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProfile([FromRoute] Guid id, [FromBody] UpdateDeviceProfileCommand command)
    {
        // 🌟 依然采用极其优雅的 record with 语法，融合路由中的 ID 与 Body 负载
        var result = await Sender.Send(command with { DeviceId = id });
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 停用设备 (软删除控制)
    /// </summary>
    /// <remarks>
    /// 设备一旦停用，将在列表前端隐藏，且无法再从云端拉取任何工艺配方。
    /// </remarks>
    /// <param name="id">设备 ID</param>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Deactivate([FromRoute] Guid id)
    {
        var command = new DeactivateDeviceCommand(id);
        var result = await Sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }
}