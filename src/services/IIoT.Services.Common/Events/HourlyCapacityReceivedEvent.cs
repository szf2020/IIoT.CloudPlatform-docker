namespace IIoT.Services.Common.Events;

/// <summary>
/// 事件：半小时槽位产能已接收
/// 由 HttpApi 的 ReceiveHourlyCapacityCommand 发布，由 DataWorker 中的 Consumer 订阅处理
/// </summary>
public record HourlyCapacityReceivedEvent
{
    public Guid DeviceId { get; init; }
    public DateOnly Date { get; init; }
    public int Hour { get; init; }
    public int Minute { get; init; }
    public string TimeLabel { get; init; } = string.Empty;
    public string ShiftCode { get; init; } = string.Empty;
    public int TotalCount { get; init; }
    public int OkCount { get; init; }
    public int NgCount { get; init; }
}
