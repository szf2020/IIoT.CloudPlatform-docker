using IIoT.Core.Production.Aggregates.Devices;
using IIoT.SharedKernel.Specification;
using System;
using System.Collections.Generic;

namespace IIoT.Core.Production.Specifications.Devices;

/// <summary>
/// 专用查询规约：查询设备分页列表 (支持双维管辖权并集过滤 + 关键字搜索)
/// </summary>
public class DevicePagedSpec : Specification<Device>
{
    /// <param name="skip">跳过条数</param>
    /// <param name="take">获取条数</param>
    /// <param name="allowedProcessIds">允许查询的工序ID集合 (若为null则代表上帝视角查全库)</param>
    /// <param name="allowedDeviceIds">允许查询的设备ID集合 (单独分配的设备管辖权)</param>
    /// <param name="keyword">关键字 (匹配设备名称)</param>
    /// <param name="isPaging">是否启用分页 (查总数时传 false)</param>
    public DevicePagedSpec(
        int skip,
        int take,
        List<Guid>? allowedProcessIds = null,
        List<Guid>? allowedDeviceIds = null,
        string? keyword = null,
        bool isPaging = true)
    {
        FilterCondition = d =>
            (
                (allowedProcessIds == null && allowedDeviceIds == null)
                ||
                (allowedProcessIds != null && allowedProcessIds.Contains(d.ProcessId))
                ||
                (allowedDeviceIds != null && allowedDeviceIds.Contains(d.Id))
            )
            &&
            (string.IsNullOrEmpty(keyword) || d.DeviceName.Contains(keyword));

        SetOrderBy(d => d.DeviceName);

        if (isPaging)
        {
            SetPaging(skip, take);
        }
    }
}