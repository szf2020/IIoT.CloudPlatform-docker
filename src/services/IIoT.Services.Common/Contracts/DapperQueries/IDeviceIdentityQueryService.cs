namespace IIoT.Services.Common.Contracts.DapperQueries;

/// <summary>
/// 设备身份快照:仅包含 Persist 和 Receive 阶段所需的最小身份
/// 三元组(DeviceId + MacAddress + ClientCode),不含 DeviceName /
/// ProcessId 等业务档案字段,避免缓存承载不必要信息。
/// </summary>
public sealed record DeviceIdentitySnapshot(
    Guid DeviceId,
    string MacAddress,
    string ClientCode);

/// <summary>
/// 设备身份查询服务契约(Dapper 实现,带 FusionCache 缓存)。
/// 供 DataWorker Persist 用例和 HttpApi Receive 用例共用,
/// 是云端寻址的主流量路径之一,必须走缓存保护 DB。
/// </summary>
public interface IDeviceIdentityQueryService
{
    /// <summary>
    /// 按 DeviceId 反查设备身份三元组。缓存未命中才查 DB。
    /// </summary>
    Task<DeviceIdentitySnapshot?> GetByDeviceIdAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 判断 DeviceId 对应的设备是否存在。
    /// 内部复用 GetByDeviceIdAsync 的缓存,不走独立查询。
    /// </summary>
    Task<bool> ExistsAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default);
}
