namespace IIoT.Core.Production.Contracts.RecordRepositories;

/// <summary>
/// Write model for pass-station injection records.
/// </summary>
public sealed record PassDataInjectionWriteModel(
    Guid Id,
    Guid DeviceId,
    string CellResult,
    DateTime CompletedTime,
    DateTime ReceivedAt,
    string Barcode,
    DateTime PreInjectionTime,
    decimal PreInjectionWeight,
    DateTime PostInjectionTime,
    decimal PostInjectionWeight,
    decimal InjectionVolume);

public interface IPassDataInjectionRecordRepository
{
    Task InsertBatchAsync(
        IReadOnlyCollection<PassDataInjectionWriteModel> items,
        CancellationToken cancellationToken = default);
}
