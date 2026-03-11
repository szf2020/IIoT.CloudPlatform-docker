using IIoT.Core.Employee.Aggregates.Employees;
using IIoT.Core.Employee.Specifications;
using IIoT.Core.Production.Aggregates.Devices;
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

namespace IIoT.ProductionService.Queries.Devices;

/// <summary>
/// 纯净的列表展示 DTO
/// </summary>
public record DeviceListItemDto(
    Guid Id,
    string DeviceName,
    string DeviceCode,
    Guid ProcessId,
    bool IsActive
);

/// <summary>
/// 交互查询：获取“我管辖范围内”的设备分页列表
/// </summary>
[AuthorizeRequirement("Device.Read")] // 🌟 第一道门拦截
public record GetMyDevicesPagedQuery(Pagination PaginationParams, string? Keyword = null) : IQuery<Result<PagedList<DeviceListItemDto>>>;

public class GetMyDevicesPagedHandler(
    ICurrentUser currentUser,
    IReadRepository<Employee> employeeRepository,
    IReadRepository<Device> deviceRepository
) : IQueryHandler<GetMyDevicesPagedQuery, Result<PagedList<DeviceListItemDto>>>
{
    public async Task<Result<PagedList<DeviceListItemDto>>> Handle(GetMyDevicesPagedQuery request, CancellationToken cancellationToken)
    {
        List<Guid>? allowedProcessIds = null;

        // ==========================================
        // 🌟 第二道门：动态计算数据管辖权 (ABAC)
        // ==========================================
        if (currentUser.Role != "Admin")
        {
            if (!Guid.TryParse(currentUser.Id, out var userId)) return Result.Failure("用户凭证异常");

            // 复用员工模块的规约图纸，极速拉取员工的工序管辖权
            var employeeSpec = new EmployeeWithAccessesSpec(userId);
            var employee = await employeeRepository.GetSingleOrDefaultAsync(employeeSpec, cancellationToken);

            if (employee == null) return Result.Failure("系统中未找到您的员工档案");

            // 提取该员工被授权的所有工序 ID
            allowedProcessIds = employee.ProcessAccesses.Select(p => p.ProcessId).ToList();

            // 如果非 Admin 且没有任何工序管辖权，直接返回空列表，不查数据库
            if (allowedProcessIds.Count == 0)
            {
                var emptyList = new PagedList<DeviceListItemDto>([], 0, request.PaginationParams);
                return Result.Success(emptyList);
            }
        }

        // ==========================================
        // 🌟 组装规约与并发查询
        // ==========================================
        var skip = (request.PaginationParams.PageNumber - 1) * request.PaginationParams.PageSize;
        var take = request.PaginationParams.PageSize;

        // 1. 数据列表规约 (开启分页)
        var pagedSpec = new DevicePagedSpec(skip, take, allowedProcessIds, request.Keyword, isPaging: true);

        // 2. 总数统计规约 (关闭分页)
        var countSpec = new DevicePagedSpec(0, 0, allowedProcessIds, request.Keyword, isPaging: false);

        // 🌟 极致性能：并发执行获取数据与统计总数
        var listTask = deviceRepository.GetListAsync(pagedSpec, cancellationToken);
        var countTask = deviceRepository.CountAsync(countSpec, cancellationToken);

        await Task.WhenAll(listTask, countTask);

        // ==========================================
        // 🌟 DTO 转换与封装
        // ==========================================
        var dtos = listTask.Result.Select(d => new DeviceListItemDto(
            d.Id,
            d.DeviceName,
            d.DeviceCode,
            d.ProcessId,
            d.IsActive
        )).ToList();

        // 完美套用底层 SharedKernel 的 PagedList 封装
        var pagedList = new PagedList<DeviceListItemDto>(dtos, countTask.Result, request.PaginationParams);

        return Result.Success(pagedList);
    }
}