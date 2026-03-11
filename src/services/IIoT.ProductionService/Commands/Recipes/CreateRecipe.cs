using IIoT.Application.Contracts;
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
/// 业务指令：创建新的生产配方 (初始版本默认 V1.0)
/// </summary>
[AuthorizeRequirement("Recipe.Create")]
public record CreateRecipeCommand(
    string RecipeName,
    Guid ProcessId,
    Guid? DeviceId,
    string ParametersJsonb
// 🔪 移除了 Version，遵循领域模型内部的初始版本规则
) : ICommand<Result<Guid>>;

public class CreateRecipeHandler(
    ICurrentUser currentUser,
    IReadRepository<Employee> employeeRepository,
    IDataQueryService dataQueryService,
    IRepository<Recipe> recipeRepository,
    ICacheService cacheService
) : ICommandHandler<CreateRecipeCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateRecipeCommand request, CancellationToken cancellationToken)
    {
        // ==========================================
        // 🌟 1. 极速无锁校验区
        // ==========================================

        var processExists = await dataQueryService.AnyAsync(
            dataQueryService.MfgProcesses.Where(p => p.Id == request.ProcessId)
        );
        if (!processExists) return Result.Failure("配方创建失败：指定的归属工序不存在");

        if (request.DeviceId.HasValue)
        {
            var deviceValid = await dataQueryService.AnyAsync(
                dataQueryService.Devices.Where(d => d.Id == request.DeviceId.Value && d.ProcessId == request.ProcessId)
            );
            if (!deviceValid) return Result.Failure("配方创建失败：指定的机台不存在或不属于当前工序");
        }

        // 🌟 修正防重校验：直接校验默认的 "V1.0" 版本是否已被占用
        var duplicateExists = await dataQueryService.AnyAsync(
            dataQueryService.Recipes.Where(r =>
                r.ProcessId == request.ProcessId &&
                r.DeviceId == request.DeviceId &&
                r.RecipeName == request.RecipeName &&
                r.Version == "V1.0")
        );
        if (duplicateExists) return Result.Failure($"配方创建失败：已存在同名的 V1.0 初始版本配方 [{request.RecipeName}]");

        // ==========================================
        // 🌟 2. 动态双维管辖权极其严格的拦截 (ABAC)
        // ==========================================
        if (currentUser.Role != "Admin")
        {
            if (!Guid.TryParse(currentUser.Id, out var userId)) return Result.Failure("用户凭证异常");

            var employeeSpec = new EmployeeWithAccessesSpec(userId);
            var employee = await employeeRepository.GetSingleOrDefaultAsync(employeeSpec, cancellationToken);
            if (employee == null) return Result.Failure("系统中未找到您的员工档案");

            if (request.DeviceId.HasValue)
            {
                var hasDeviceAccess = employee.DeviceAccesses.Any(d => d.DeviceId == request.DeviceId.Value);
                if (!hasDeviceAccess) return Result.Failure("越权警告：您没有该具体机台的管辖权，无法为其创建特调配方！");
            }
            else
            {
                var hasProcessAccess = employee.ProcessAccesses.Any(p => p.ProcessId == request.ProcessId);
                if (!hasProcessAccess) return Result.Failure("越权警告：您没有该工序的管辖权，无法创建通用配方！");
            }
        }

        // ==========================================
        // 🌟 3. 领域对象构建与持久化 (严格遵照您的构造函数定义)
        // ==========================================
        var recipe = new Recipe(
            request.RecipeName,
            request.ProcessId,
            request.ParametersJsonb, // 第三个参数是 Jsonb
            request.DeviceId         // 第四个参数是选填的机台 ID
        );

        recipeRepository.Add(recipe);
        var affected = await recipeRepository.SaveChangesAsync(cancellationToken);

        // ==========================================
        // 🌟 4. 缓存一致性保障
        // ==========================================
        if (affected > 0)
        {
            await cacheService.RemoveAsync($"iiot:recipes:process:v1:{request.ProcessId}", cancellationToken);
            if (request.DeviceId.HasValue)
            {
                await cacheService.RemoveAsync($"iiot:recipes:device:v1:{request.DeviceId.Value}", cancellationToken);
            }
        }

        return Result.Success(recipe.Id);
    }
}