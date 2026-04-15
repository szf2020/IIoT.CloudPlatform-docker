using IIoT.Services.Common.Caching;
using IIoT.Services.Common.Contracts;
using IIoT.Services.Common.Contracts.Authorization;
using Microsoft.EntityFrameworkCore;

namespace IIoT.EntityFrameworkCore.Identity;

public sealed class DevicePermissionService(IIoTDbContext dbContext, ICacheService cacheService)
    : IDevicePermissionService
{
    public async Task<IReadOnlyList<Guid>?> GetAccessibleDeviceIdsAsync(
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        if (isAdmin)
        {
            return null;
        }

        var cacheKey = CacheKeys.DeviceAccessesByUser(userId);
        var cached = await cacheService.GetAsync<List<Guid>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var accessibleDeviceIds = await dbContext.Employees
            .Where(employee => employee.Id == userId)
            .SelectMany(employee => employee.DeviceAccesses.Select(deviceAccess => deviceAccess.DeviceId))
            .Distinct()
            .ToListAsync(cancellationToken);

        await cacheService.SetAsync(cacheKey, accessibleDeviceIds, TimeSpan.FromMinutes(10), cancellationToken);

        return accessibleDeviceIds;
    }
}
