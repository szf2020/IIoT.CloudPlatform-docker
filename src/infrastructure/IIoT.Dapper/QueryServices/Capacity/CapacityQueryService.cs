using Dapper;
using IIoT.Dapper;
using IIoT.Services.Common.Contracts.DapperQueries;
using IIoT.SharedKernel.Paging;

namespace IIoT.Dapper.QueryServices.Capacity;

public class CapacityQueryService(IDbConnectionFactory connectionFactory) : ICapacityQueryService
{
    public async Task<(List<dynamic> Items, int TotalCount)> GetDailyPagedAsync(
        Pagination pagination,
        Guid? deviceId = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();

        var conditions = "WHERE 1=1";
        var parameters = new DynamicParameters();

        if (deviceId.HasValue)
        {
            conditions += " AND device_id = @DeviceId";
            parameters.Add("DeviceId", deviceId.Value);
        }

        if (startDate.HasValue)
        {
            conditions += " AND date >= @StartDate";
            parameters.Add("StartDate", startDate.Value);
        }

        if (endDate.HasValue)
        {
            conditions += " AND date <= @EndDate";
            parameters.Add("EndDate", endDate.Value);
        }

        var dataSql = $@"
            SELECT id, device_id, date, shift_code, 
                   total_count, ok_count, ng_count, reported_at
            FROM daily_capacity
            {conditions}
            ORDER BY date DESC, shift_code
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var countSql = $"SELECT COUNT(*) FROM daily_capacity {conditions}";

        var offset = (pagination.PageNumber - 1) * pagination.PageSize;
        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pagination.PageSize);

        var command = new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken);
        var countCommand = new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken);

        var items = (await connection.QueryAsync(command)).ToList();
        var totalCount = await connection.ExecuteScalarAsync<int>(countCommand);

        return (items, totalCount);
    }

    public async Task<List<dynamic>> GetSummaryAsync(
        Guid? deviceId = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();

        var conditions = "WHERE 1=1";
        var parameters = new DynamicParameters();

        if (deviceId.HasValue)
        {
            conditions += " AND device_id = @DeviceId";
            parameters.Add("DeviceId", deviceId.Value);
        }

        if (startDate.HasValue)
        {
            conditions += " AND date >= @StartDate";
            parameters.Add("StartDate", startDate.Value);
        }

        if (endDate.HasValue)
        {
            conditions += " AND date <= @EndDate";
            parameters.Add("EndDate", endDate.Value);
        }

        var sql = $@"
            SELECT device_id,
                   SUM(total_count) AS total_count,
                   SUM(ok_count) AS ok_count,
                   SUM(ng_count) AS ng_count
            FROM daily_capacity
            {conditions}
            GROUP BY device_id
            ORDER BY device_id";

        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        return (await connection.QueryAsync(command)).ToList();
    }
}