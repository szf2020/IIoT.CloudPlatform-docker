using IIoT.SharedKernel.Domain;

namespace IIoT.Core.Production.Aggregates.Capacities;

/// <summary>
/// 聚合根：半小时产能汇总记录
/// 唯一约束：DeviceId + Date + Hour + Minute + ShiftCode
/// </summary>
public class HourlyCapacity : IAggregateRoot
{
    protected HourlyCapacity()
    {
    }

    public HourlyCapacity(
        Guid deviceId,
        DateOnly date,
        string shiftCode,
        int hour,
        int minute,
        string timeLabel,
        int totalCount,
        int okCount,
        int ngCount)
    {
        Id = Guid.NewGuid();
        DeviceId = deviceId;
        Date = date;
        ShiftCode = shiftCode;
        Hour = hour;
        Minute = minute;
        TimeLabel = timeLabel;
        TotalCount = totalCount;
        OkCount = okCount;
        NgCount = ngCount;
        ReportedAt = DateTime.UtcNow;
    }

    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public DateOnly Date { get; set; }
    public string ShiftCode { get; set; } = null!;
    public int Hour { get; set; }
    public int Minute { get; set; }
    public string TimeLabel { get; set; } = null!;
    public int TotalCount { get; set; }
    public int OkCount { get; set; }
    public int NgCount { get; set; }
    public DateTime ReportedAt { get; set; }

    public void UpdateCapacity(int totalCount, int okCount, int ngCount)
    {
        TotalCount = totalCount;
        OkCount = okCount;
        NgCount = ngCount;
        ReportedAt = DateTime.UtcNow;
    }
}
