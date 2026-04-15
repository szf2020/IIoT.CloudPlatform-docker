namespace IIoT.Services.Common.Contracts.RecordQueries;

public interface IDeviceReadQueryService
{
    Task<bool> ExistsAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsInProcessAsync(
        Guid deviceId,
        Guid processId,
        CancellationToken cancellationToken = default);

    Task<bool> InstanceExistsAsync(
        string macAddress,
        string clientCode,
        Guid? excludingDeviceId = null,
        CancellationToken cancellationToken = default);
}
