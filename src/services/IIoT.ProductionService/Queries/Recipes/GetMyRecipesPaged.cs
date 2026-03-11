using IIoT.Core.Employee.Aggregates.Employees;
using IIoT.Core.Employee.Specifications;
using IIoT.Core.Production.Aggregates.Recipes;
using IIoT.Core.Production.Specifications;
using IIoT.Services.Common.Attributes;
using IIoT.Services.Common.Contracts;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Paging;
using IIoT.SharedKernel.Repository;
using IIoT.SharedKernel.Result;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IIoT.ProductionService.Queries.Recipes;

/// <summary>
/// 纯净的配方列表展示 DTO (绝不暴露 ParametersJsonb 等敏感或超大字段)
/// </summary>
public record RecipeListItemDto(
    Guid Id,
    string RecipeName,
    string Version,
    Guid ProcessId,
    Guid? DeviceId, // null 代表它是通用配方
    bool IsActive
);

/// <summary>
/// 交互查询：获取“我管辖范围内”的配方分页列表 (支持搜索)
/// </summary>
[AuthorizeRequirement("Recipe.Read")] // 🌟 第一道门：基础行为拦截
public record GetMyRecipesPagedQuery(
    Pagination PaginationParams,
    string? Keyword = null
) : IQuery<Result<PagedList<RecipeListItemDto>>>;

public class GetMyRecipesPagedHandler(
    ICurrentUser currentUser,
    IReadRepository<Employee> employeeRepository, // 极速读取员工权限
    IReadRepository<Recipe> recipeRepository      // 纯净的配方读仓储
) : IQueryHandler<GetMyRecipesPagedQuery, Result<PagedList<RecipeListItemDto>>>
{
    public async Task<Result<PagedList<RecipeListItemDto>>> Handle(GetMyRecipesPagedQuery request, CancellationToken cancellationToken)
    {
        List<Guid>? allowedProcessIds = null;
        List<Guid>? allowedDeviceIds = null;

        // ==========================================
        // 🌟 1. 第二道门：动态拉取数据管辖权 (ABAC)
        // ==========================================
        if (currentUser.Role != "Admin") // Admin 保持双 null，规约底层自动识别为上帝视角
        {
            if (!Guid.TryParse(currentUser.Id, out var userId)) return Result.Failure("用户凭证异常");

            // 复用员工模块的规约，极速拉取双维管辖权
            var employeeSpec = new EmployeeWithAccessesSpec(userId);
            var employee = await employeeRepository.GetSingleOrDefaultAsync(employeeSpec, cancellationToken);

            if (employee == null) return Result.Failure("系统中未找到您的员工档案");

            // 提取被授权的集合
            allowedProcessIds = employee.ProcessAccesses.Select(p => p.ProcessId).ToList();
            allowedDeviceIds = employee.DeviceAccesses.Select(d => d.DeviceId).ToList();

            // 🌟 性能防御极速阻断：如果啥权限都没有，连数据库都不用查了，直接返回空分页
            if (allowedProcessIds.Count == 0 && allowedDeviceIds.Count == 0)
            {
                var emptyList = new PagedList<RecipeListItemDto>([], 0, request.PaginationParams);
                return Result.Success(emptyList);
            }
        }

        // ==========================================
        // 🌟 2. 组装规约图纸与并发极致压榨
        // ==========================================
        var skip = (request.PaginationParams.PageNumber - 1) * request.PaginationParams.PageSize;
        var take = request.PaginationParams.PageSize;

        // 图纸 A：数据列表规约 (开启分页) - 将所有权限集合与参数原封不动丢给底层图纸！
        var pagedSpec = new RecipePagedSpec(skip, take, allowedProcessIds, allowedDeviceIds, request.Keyword, isPaging: true);

        // 图纸 B：总数统计规约 (关闭分页)
        var countSpec = new RecipePagedSpec(0, 0, allowedProcessIds, allowedDeviceIds, request.Keyword, isPaging: false);

        // 🌟 极致性能：利用 Task.WhenAll 并发向 PostgreSQL 发起获取数据和 Count 统计
        var listTask = recipeRepository.GetListAsync(pagedSpec, cancellationToken);
        var countTask = recipeRepository.CountAsync(countSpec, cancellationToken);

        await Task.WhenAll(listTask, countTask);

        // ==========================================
        // 🌟 3. DTO 转换与封装返回
        // ==========================================
        // 剥离领域聚合根实体，只返回前端列表需要的轻量级字段
        var dtos = listTask.Result.Select(r => new RecipeListItemDto(
            r.Id,
            r.RecipeName,
            r.Version,
            r.ProcessId,
            r.DeviceId,
            r.IsActive
        )).ToList();

        // 完美套用 SharedKernel 提供的 PagedList 封装
        var pagedList = new PagedList<RecipeListItemDto>(dtos, countTask.Result, request.PaginationParams);

        return Result.Success(pagedList);
    }
}