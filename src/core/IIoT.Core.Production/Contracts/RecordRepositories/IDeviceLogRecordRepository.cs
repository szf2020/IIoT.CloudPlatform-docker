namespace IIoT.Core.Production.Contracts.RecordRepositories;

/// <summary>
/// Write model for device logs. Identity is represented only by device_id.
/// </summary>
public sealed record DeviceLogWriteModel(
    Guid Id,
    Guid DeviceId,
    string Level,
    string Message,
    DateTime LogTime,
    DateTime ReceivedAt,
    string IdempotencyKey);

public interface IDeviceLogRecordRepository
{
    Task InsertBatchAsync(
        IReadOnlyCollection<DeviceLogWriteModel> items,
        CancellationToken cancellationToken = default);
}
