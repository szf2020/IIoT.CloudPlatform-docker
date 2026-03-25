using IIoT.SharedKernel.Paging;

namespace IIoT.Services.Common.Contracts.DapperQueries;

/// <summary>
/// 设备日志查询服务契约（Dapper 实现）
/// </summary>
public interface IDeviceLogQueryService
{
    /// <summary>
    /// 分页查询设备日志（可按设备、级别、时间范围筛选）
    /// </summary>
    Task<(List<dynamic> Items, int TotalCount)> GetPagedAsync(
        Pagination pagination,
        Guid? deviceId = null,
        string? level = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default);
}