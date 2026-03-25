using IIoT.SharedKernel.Paging;

namespace IIoT.Services.Common.Contracts.DapperQueries;

/// <summary>
/// 过站数据查询服务契约（Dapper 实现）
/// 云端后台管理端分页查询使用，列表查询不返回大字段
/// </summary>
public interface IPassStationQueryService
{
    /// <summary>
    /// 分页查询注液工序过站数据
    /// </summary>
    Task<(List<dynamic> Items, int TotalCount)> GetInjectionPagedAsync(
        Pagination pagination,
        Guid? deviceId = null,
        string? barcode = null,
        string? cellResult = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据 ID 获取单条过站数据详情（含全部字段）
    /// </summary>
    Task<dynamic?> GetInjectionDetailAsync(Guid id, CancellationToken cancellationToken = default);
}