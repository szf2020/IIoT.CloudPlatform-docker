namespace IIoT.Services.Common.Events;

/// <summary>
/// 注液工序过站数据接收事件(批量)。
/// HttpApi 接收上位机上报后发布,DataWorker 消费后批量落库。
/// 一个上位机进程对应一个 Device,一批过站数据共享同一 DeviceId,
/// 因此 DeviceId 在事件顶层只出现一次,Items 集合内每条不再重复携带。
/// </summary>
public record PassDataInjectionReceivedEvent
{
    public Guid DeviceId { get; init; }
    public List<PassDataInjectionItem> Items { get; init; } = [];
}

/// <summary>
/// 单条注液过站数据条目。不带 DeviceId — 整批共享
/// <see cref="PassDataInjectionReceivedEvent.DeviceId"/>。
/// </summary>
public record PassDataInjectionItem
{
    public string Barcode { get; init; } = string.Empty;
    public string CellResult { get; init; } = string.Empty;
    public DateTime CompletedTime { get; init; }
    public DateTime PreInjectionTime { get; init; }
    public decimal PreInjectionWeight { get; init; }
    public DateTime PostInjectionTime { get; init; }
    public decimal PostInjectionWeight { get; init; }
    public decimal InjectionVolume { get; init; }
}