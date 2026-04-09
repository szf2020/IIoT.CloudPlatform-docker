namespace IIoT.Services.Common.Events;

/// <summary>
/// 注液工序过站数据接收事件(单条)。
/// HttpApi 接收上位机上报后发布,DataWorker 消费后落库。
/// 上位机身份已在轮询认证接口换取 DeviceId,后续数据流转全部以 DeviceId 为唯一标识。
/// </summary>
public record PassDataInjectionReceivedEvent
{
    public Guid DeviceId { get; init; }

    public string Barcode { get; init; } = string.Empty;
    public string CellResult { get; init; } = string.Empty;
    public DateTime CompletedTime { get; init; }
    public DateTime PreInjectionTime { get; init; }
    public decimal PreInjectionWeight { get; init; }
    public DateTime PostInjectionTime { get; init; }
    public decimal PostInjectionWeight { get; init; }
    public decimal InjectionVolume { get; init; }
}