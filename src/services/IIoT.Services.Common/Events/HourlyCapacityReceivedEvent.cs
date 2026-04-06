namespace IIoT.Services.Common.Events;

public record HourlyCapacityReceivedEvent
{
    public Guid DeviceId { get; init; }
    public DateOnly Date { get; init; }
    public string ShiftCode { get; init; } = string.Empty;
    public int Hour { get; init; }
    public int Minute { get; init; }
    public string TimeLabel { get; init; } = string.Empty;
    public int TotalCount { get; init; }
    public int OkCount { get; init; }
    public int NgCount { get; init; }

    /// <summary>
    /// 产生该产能数据的 PLC 名称（可空，Edge 端不传时为 null）
    /// </summary>
    public string? PlcName { get; init; }
}