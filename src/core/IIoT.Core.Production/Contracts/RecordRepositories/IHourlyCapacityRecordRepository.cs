namespace IIoT.Core.Production.Contracts.RecordRepositories;

/// <summary>
/// Write model for half-hour capacity records.
/// </summary>
public sealed record HourlyCapacityWriteModel(
    Guid Id,
    Guid DeviceId,
    DateOnly Date,
    string ShiftCode,
    int Hour,
    int Minute,
    string TimeLabel,
    int TotalCount,
    int OkCount,
    int NgCount,
    string PlcName,
    DateTime ReportedAt);

public interface IHourlyCapacityRecordRepository
{
    Task UpsertAsync(
        HourlyCapacityWriteModel item,
        CancellationToken cancellationToken = default);
}
