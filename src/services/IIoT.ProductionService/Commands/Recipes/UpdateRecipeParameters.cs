using IIoT.Core.Employee.Aggregates.Employees;
using IIoT.Core.Employee.Specifications;
using IIoT.Core.Production.Aggregates.Recipes;
using IIoT.Services.Common.Attributes;
using IIoT.Services.Common.Contracts;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Repository;
using IIoT.SharedKernel.Result;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IIoT.ProductionService.Commands.Recipes;

/// <summary>
/// 业务指令：更新配方的 JSONB 工艺参数并升级版本号
/// </summary>
[AuthorizeRequirement("Recipe.Update")] // 🌟 第一道门：基础行为权限拦截
public record UpdateRecipeParametersCommand(
    Guid RecipeId,
    string ParametersJsonb,
    string Version
) : ICommand<Result<bool>>;

public class UpdateRecipeParametersHandler(
    ICurrentUser currentUser,
    IReadRepository<Employee> employeeRepository, // 拉取管辖权
    IDataQueryService dataQueryService,           // 极速无锁校验
    IRepository<Recipe> recipeRepository,         // 实体变更仓储
    ICacheService cacheService                    // 缓存双杀爆破
) : ICommandHandler<UpdateRecipeParametersCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(UpdateRecipeParametersCommand request, CancellationToken cancellationToken)
    {
        // ==========================================
        // 🌟 1. 获取目标聚合根
        // ==========================================
        var recipe = await recipeRepository.GetByIdAsync(request.RecipeId, cancellationToken);
        if (recipe == null) return Result.Failure("更新失败：目标配方不存在");

        // ==========================================
        // 🌟 2. 第二道门：动态双维数据管辖权绝对拦截 (ABAC)
        // ==========================================
        if (currentUser.Role != "Admin")
        {
            if (!Guid.TryParse(currentUser.Id, out var userId)) return Result.Failure("用户凭证异常");

            var employeeSpec = new EmployeeWithAccessesSpec(userId);
            var employee = await employeeRepository.GetSingleOrDefaultAsync(employeeSpec, cancellationToken);
            if (employee == null) return Result.Failure("系统中未找到您的员工档案");

            // 🌟 核心灵魂：根据底层配方的物理归属，动态判定校验规则！
            if (recipe.DeviceId.HasValue)
            {
                // 场景 A: 这是一个特调配方 -> 必须拥有该【机台】的管辖权
                var hasDeviceAccess = employee.DeviceAccesses.Any(d => d.DeviceId == recipe.DeviceId.Value);
                if (!hasDeviceAccess) return Result.Failure("越权警告：您没有该具体机台的管辖权，严禁修改此特调配方！");
            }
            else
            {
                // 场景 B: 这是一个通用配方 -> 必须拥有该【工序】的管辖权
                var hasProcessAccess = employee.ProcessAccesses.Any(p => p.ProcessId == recipe.ProcessId);
                if (!hasProcessAccess) return Result.Failure("越权警告：您没有该工序的管辖权，严禁修改此通用配方！");
            }
        }

        // ==========================================
        // 🌟 3. 版本号防重校验 (极速无锁)
        // ==========================================
        // 只有当版本号发生变更时，才需要校验新版本号是否与现有同级配方冲突
        if (recipe.Version != request.Version)
        {
            var duplicateExists = await dataQueryService.AnyAsync(
                dataQueryService.Recipes.Where(r =>
                    r.ProcessId == recipe.ProcessId &&
                    r.DeviceId == recipe.DeviceId &&
                    r.RecipeName == recipe.RecipeName &&
                    r.Version == request.Version &&
                    r.Id != recipe.Id) // 排除自身
            );

            if (duplicateExists) return Result.Failure($"配方更新失败：已存在名为 [{recipe.RecipeName}] 的 {request.Version} 版本");
        }

        // ==========================================
        // 🌟 4. 领域行为执行与持久化
        // ==========================================
        // 严格遵循充血模型：调用您定义的领域方法，坚决不直接使用 set 赋值
        recipe.UpdateParameters(request.ParametersJsonb, request.Version);

        recipeRepository.Update(recipe);
        var affected = await recipeRepository.SaveChangesAsync(cancellationToken);

        // ==========================================
        // 🌟 5. 缓存强一致性保障：三杀爆破！
        // ==========================================
        if (affected > 0)
        {
            // 1. 爆破单体详情缓存
            await cacheService.RemoveAsync($"iiot:recipe:v1:{recipe.Id}", cancellationToken);

            // 2. 爆破该工序下的通用配方列表缓存
            await cacheService.RemoveAsync($"iiot:recipes:process:v1:{recipe.ProcessId}", cancellationToken);

            // 3. 如果是特调配方，连带爆破机台专属列表缓存
            if (recipe.DeviceId.HasValue)
            {
                await cacheService.RemoveAsync($"iiot:recipes:device:v1:{recipe.DeviceId.Value}", cancellationToken);
            }
        }

        return Result.Success(true);
    }
}