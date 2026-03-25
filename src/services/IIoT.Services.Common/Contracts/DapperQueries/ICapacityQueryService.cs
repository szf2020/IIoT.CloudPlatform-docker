using IIoT.SharedKernel.Paging;

namespace IIoT.Services.Common.Contracts.DapperQueries;

/// <summary>
/// 产能汇总查询服务契约（Dapper 实现）
/// </summary>
public interface ICapacityQueryService
{
    /// <summary>
    /// 分页查询每日产能汇总
    /// </summary>
    Task<(List<dynamic> Items, int TotalCount)> GetDailyPagedAsync(
        Pagination pagination,
        Guid? deviceId = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 按设备/时间段聚合统计（云端大屏用）
    /// </summary>
    Task<List<dynamic>> GetSummaryAsync(
        Guid? deviceId = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        CancellationToken cancellationToken = default);
}