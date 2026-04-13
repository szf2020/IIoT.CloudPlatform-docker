using IIoT.HttpApi.Infrastructure;
using IIoT.ProductionService.Commands.Recipes;
using IIoT.ProductionService.Queries.Recipes;
using IIoT.SharedKernel.Paging;
using Microsoft.AspNetCore.Mvc;

namespace IIoT.HttpApi.Controllers;

/// <summary>
/// 配方接口。
/// </summary>
[Route("api/v1/[controller]")]
[ApiController]
[Tags("配方科 - 生产工艺与参数")]
public class RecipeController : ApiControllerBase
{
    /// <summary>
    /// 获取配方分页列表。
    [HttpGet]
    public async Task<IActionResult> GetPagedList([FromQuery] Pagination pagination, [FromQuery] string? keyword = null)
    {
        var query = new GetMyRecipesPagedQuery(pagination, keyword);
        var result = await Sender.Send(query);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 获取配方详情。
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetail([FromRoute] Guid id)
    {
        var query = new GetRecipeByIdQuery(id);
        var result = await Sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 创建配方。
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRecipeCommand command)
    {
        var result = await Sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 升级配方版本。
    [HttpPost("{id}/upgrade")]
    public async Task<IActionResult> UpgradeVersion([FromRoute] Guid id, [FromBody] UpgradeRecipeVersionCommand command)
    {
        var result = await Sender.Send(command with { SourceRecipeId = id });
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 删除配方。
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        var command = new DeleteRecipeCommand(id);
        var result = await Sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// 获取设备可用配方列表。
    [HttpGet("device/{deviceId}")]
    public async Task<IActionResult> GetByDeviceId([FromRoute] Guid deviceId)
    {
        var query = new GetRecipesByDeviceIdQuery(deviceId);
        var result = await Sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }
}
