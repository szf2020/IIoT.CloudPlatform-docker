using IIoT.SharedKernel.Domain;

namespace IIoT.Core.Production.Aggregates.Capacities;

/// <summary>
/// 聚合根：半小时槽位产能记录
/// 边缘端实时上报半小时聚合产能数据
/// 唯一约束：DeviceId + Date + Hour + Minute + ShiftCode (同一设备同一时段同一班次不允许重复，支持 Upsert 覆盖)
/// </summary>
public class HourlyCapacity : IAggregateRoot
{
    protected HourlyCapacity()
    {
    }

    public HourlyCapacity(
        Guid deviceId,
        DateOnly date,
        int hour,
        int minute,
        string timeLabel,
        string shiftCode,
        int totalCount,
        int okCount,
        int ngCount)
    {
        Id = Guid.NewGuid();
        DeviceId = deviceId;
        Date = date;
        Hour = hour;
        Minute = minute;
        TimeLabel = timeLabel;
        ShiftCode = shiftCode;
        TotalCount = totalCount;
        OkCount = okCount;
        NgCount = ngCount;
        ReportedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 产能记录全局唯一标识 (UUID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 上报设备 UUID (关联 devices 表)
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// 统计日期 (如: 2026-04-01，按 CompletedTime 记录完成时间的业务日期)
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// 半小时槽起点 - 小时 (0-23)
    /// </summary>
    public int Hour { get; set; }

    /// <summary>
    /// 半小时槽起点 - 分钟 (0 或 30)
    /// </summary>
    public int Minute { get; set; }

    /// <summary>
    /// 展示用时段标签 (如: "09:30-10:00")
    /// </summary>
    public string TimeLabel { get; set; } = null!;

    /// <summary>
    /// 班次编码 (如: D / N / A / B，由工厂自行定义，客户端已判定)
    /// </summary>
    public string ShiftCode { get; set; } = null!;

    /// <summary>
    /// 该半小时槽累计总产出数量
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 良品数量
    /// </summary>
    public int OkCount { get; set; }

    /// <summary>
    /// 不良品数量
    /// </summary>
    public int NgCount { get; set; }

    /// <summary>
    /// 上报时刻 (UTC)
    /// </summary>
    public DateTime ReportedAt { get; set; }

    /// <summary>
    /// 更新产能数据（支持 Upsert 覆盖）
    /// </summary>
    public void UpdateCapacity(int totalCount, int okCount, int ngCount)
    {
        TotalCount = totalCount;
        OkCount = okCount;
        NgCount = ngCount;
        ReportedAt = DateTime.UtcNow;
    }
}
