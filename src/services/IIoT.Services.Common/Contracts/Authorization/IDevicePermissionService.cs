using IIoT.Services.Common.Contracts;

namespace IIoT.Services.Common.Contracts.Authorization;

public interface IDevicePermissionService
{
    Task<IReadOnlyList<Guid>?> GetAccessibleDeviceIdsAsync(
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);
}
