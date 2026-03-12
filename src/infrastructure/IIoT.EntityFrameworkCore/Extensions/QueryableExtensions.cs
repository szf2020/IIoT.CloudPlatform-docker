using Microsoft.EntityFrameworkCore;
using IIoT.SharedKernel.Paging;

namespace IIoT.EntityFrameworkCore.Extensions;

public static class QueryableExtensions
{
    public static async Task<PagedList<T>?> ToPageListAsync<T>(
        this IQueryable<T> queryable,
        Pagination pagination) where T : class
    {
        var count = await queryable.CountAsync();
        if (count == 0) return null;

        var items = await queryable
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        // 完全按照你知乎项目的写法，传入 pagination
        return new PagedList<T>(items, count, pagination);
    }
}