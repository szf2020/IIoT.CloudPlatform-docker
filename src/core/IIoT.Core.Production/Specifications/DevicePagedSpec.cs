using IIoT.Core.Production.Aggregates.Devices;
using IIoT.SharedKernel.Specification;
using System;
using System.Collections.Generic;

namespace IIoT.Core.Production.Specifications;

/// <summary>
/// 专用查询规约：查询设备分页列表 (支持按管辖权过滤和关键字搜索)
/// </summary>
public class DevicePagedSpec : Specification<Device>
{
    /// <summary>
    /// 构造设备分页查询规约
    /// </summary>
    /// <param name="skip">跳过条数</param>
    /// <param name="take">获取条数</param>
    /// <param name="allowedProcessIds">允许查询的工序ID集合 (若为null则代表查全库)</param>
    /// <param name="keyword">关键字 (匹配名称或编号)</param>
    /// <param name="isPaging">是否启用分页 (查总数时传 false)</param>
    public DevicePagedSpec(int skip, int take, List<Guid>? allowedProcessIds = null, string? keyword = null, bool isPaging = true)
    {
        // 🌟 核心：组合过滤条件 (管辖权过滤 + 模糊搜索)
        FilterCondition = d =>
            (allowedProcessIds == null || allowedProcessIds.Contains(d.ProcessId)) &&
            (string.IsNullOrEmpty(keyword) || d.DeviceCode.Contains(keyword) || d.DeviceName.Contains(keyword));

        // 默认按设备编号排序
        SetOrderBy(d => d.DeviceCode);

        // 如果开启分页，则装载分页参数
        if (isPaging)
        {
            SetPaging(skip, take);
        }
    }
}