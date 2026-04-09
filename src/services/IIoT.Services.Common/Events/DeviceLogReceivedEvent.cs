namespace IIoT.Services.Common.Events;

/// <summary>
/// 设备日志接收事件(批量)。
/// HttpApi 接收上位机上报后发布,DataWorker 消费后落库。
/// 一个上位机进程对应一个 Device,LogPushTask 一批推送共享同一 DeviceId,
/// 因此 DeviceId 在事件顶层只出现一次,Logs 集合内每条 Item 不再重复携带。
/// </summary>
public record DeviceLogReceivedEvent
{
    /// <summary>
    /// 本批日志归属的云端设备 ID(整批共享)。
    /// </summary>
    public Guid DeviceId { get; init; }

    /// <summary>
    /// 本批次的日志列表。
    /// </summary>
    public List<DeviceLogItem> Logs { get; init; } = [];
}

/// <summary>
/// 单条设备日志条目。
/// 不带 DeviceId — 整批共享 <see cref="DeviceLogReceivedEvent.DeviceId"/>。
/// </summary>
public record DeviceLogItem
{
    public string Level { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime LogTime { get; init; }
}