using IIoT.Services.Common.Contracts.RecordQueries;
using Microsoft.EntityFrameworkCore;

namespace IIoT.EntityFrameworkCore.QueryServices;

public sealed class DeviceReadQueryService(IIoTDbContext dbContext) : IDeviceReadQueryService
{
    public Task<bool> ExistsAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Devices
            .AsNoTracking()
            .AnyAsync(device => device.Id == deviceId, cancellationToken);
    }

    public Task<bool> ExistsInProcessAsync(
        Guid deviceId,
        Guid processId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Devices
            .AsNoTracking()
            .AnyAsync(
                device => device.Id == deviceId && device.ProcessId == processId,
                cancellationToken);
    }

    public Task<bool> InstanceExistsAsync(
        string macAddress,
        string clientCode,
        Guid? excludingDeviceId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Devices
            .AsNoTracking()
            .Where(device =>
                device.Instance.MacAddress == macAddress &&
                device.Instance.ClientCode == clientCode);

        if (excludingDeviceId.HasValue)
        {
            query = query.Where(device => device.Id != excludingDeviceId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }
}
