using Dapper;
using IIoT.Services.Common.Contracts.DapperQueries;
using IIoT.SharedKernel.Paging;

namespace IIoT.Dapper.QueryServices.Capacity;

public class CapacityQueryService(IDbConnectionFactory connectionFactory) : ICapacityQueryService
{
    public async Task<(List<dynamic> Items, int TotalCount)> GetDailyPagedAsync(
        Pagination pagination,
        DateOnly? date = null,
        Guid? deviceId = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();

        var conditions = "WHERE 1=1";
        var parameters = new DynamicParameters();

        if (date.HasValue)
        {
            conditions += " AND c.date = @Date";
            parameters.Add("Date", date.Value);
        }

        if (deviceId.HasValue)
        {
            conditions += " AND c.device_id = @DeviceId";
            parameters.Add("DeviceId", deviceId.Value);
        }

        var dataSql = $@"
    SELECT c.id, c.device_id, d.device_name,
           c.date, c.shift_code, c.total_count, c.ok_count, c.ng_count,
           CASE WHEN c.total_count > 0 
                THEN ROUND(c.ok_count * 100.0 / c.total_count, 2) 
                ELSE 0 END AS ok_rate,
           c.reported_at
    FROM daily_capacity c
    INNER JOIN devices d ON c.device_id = d.id
    {conditions}
    ORDER BY c.date DESC, d.device_name
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var countSql = $@"
            SELECT COUNT(*) 
            FROM daily_capacity c
            INNER JOIN devices d ON c.device_id = d.id
            {conditions}";

        var offset = (pagination.PageNumber - 1) * pagination.PageSize;
        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pagination.PageSize);

        var command = new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken);
        var countCommand = new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken);

        var items = (await connection.QueryAsync(command)).ToList();
        var totalCount = await connection.ExecuteScalarAsync<int>(countCommand);

        return (items, totalCount);
    }

    public async Task<List<dynamic>> GetDeviceSummaryAsync(
        Guid deviceId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();

        var sql = @"
            SELECT c.date, c.shift_code, c.total_count, c.ok_count, c.ng_count,
                   CASE WHEN c.total_count > 0 
                        THEN ROUND(c.ok_count * 100.0 / c.total_count, 2) 
                        ELSE 0 END AS ok_rate,
                   d.device_name,
                   c.reported_at
            FROM daily_capacity c
            INNER JOIN devices d ON c.device_id = d.id
            WHERE c.device_id = @DeviceId
              AND c.date >= @StartDate
              AND c.date <= @EndDate
            ORDER BY c.date DESC, c.shift_code";

        var command = new CommandDefinition(sql, new { DeviceId = deviceId, StartDate = startDate, EndDate = endDate }, cancellationToken: cancellationToken);

        return (await connection.QueryAsync(command)).ToList();
    }

    public async Task<(List<dynamic> Items, int TotalCount)> GetLastMonthByDeviceAsync(
        Guid deviceId,
        Pagination pagination,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();

        var endDate   = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = endDate.AddMonths(-1);

        var dataSql = @"
            SELECT c.id, c.device_id, d.device_name,
                   c.date, c.shift_code, c.total_count, c.ok_count, c.ng_count,
                   CASE WHEN c.total_count > 0
                        THEN ROUND(c.ok_count * 100.0 / c.total_count, 2)
                        ELSE 0 END AS ok_rate,
                   c.reported_at
            FROM daily_capacity c
            INNER JOIN devices d ON c.device_id = d.id
            WHERE c.device_id = @DeviceId
              AND c.date >= @StartDate
              AND c.date <= @EndDate
            ORDER BY c.date DESC, c.shift_code
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var countSql = @"
            SELECT COUNT(*)
            FROM daily_capacity
            WHERE device_id = @DeviceId
              AND date >= @StartDate
              AND date <= @EndDate";

        var offset = (pagination.PageNumber - 1) * pagination.PageSize;
        var parameters = new { DeviceId = deviceId, StartDate = startDate, EndDate = endDate, Offset = offset, PageSize = pagination.PageSize };

        var command      = new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken);
        var countCommand = new CommandDefinition(countSql, new { DeviceId = deviceId, StartDate = startDate, EndDate = endDate }, cancellationToken: cancellationToken);

        var items      = (await connection.QueryAsync(command)).ToList();
        var totalCount = await connection.ExecuteScalarAsync<int>(countCommand);

        return (items, totalCount);
    }

    /// <summary>
    /// 半小时槽位产能分页查询
    /// </summary>
    public async Task<(List<dynamic> Items, int TotalCount)> GetHourlyPagedAsync(
        Pagination pagination,
        DateOnly? date = null,
        Guid? deviceId = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();

        var conditions = "WHERE 1=1";
        var parameters = new DynamicParameters();

        if (date.HasValue)
        {
            conditions += " AND hc.date = @Date";
            parameters.Add("Date", date.Value);
        }

        if (deviceId.HasValue)
        {
            conditions += " AND hc.device_id = @DeviceId";
            parameters.Add("DeviceId", deviceId.Value);
        }

        var dataSql = $@"
    SELECT hc.id, hc.device_id, d.device_name,
           hc.date, hc.hour, hc.minute, hc.time_label, hc.shift_code, 
           hc.total_count, hc.ok_count, hc.ng_count,
           CASE WHEN hc.total_count > 0 
                THEN ROUND(hc.ok_count * 100.0 / hc.total_count, 2) 
                ELSE 0 END AS ok_rate,
           hc.reported_at
    FROM hourly_capacity hc
    INNER JOIN devices d ON hc.device_id = d.id
    {conditions}
    ORDER BY hc.date DESC, hc.hour DESC, hc.minute DESC, d.device_name
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var countSql = $@"
            SELECT COUNT(*) 
            FROM hourly_capacity hc
            INNER JOIN devices d ON hc.device_id = d.id
            {conditions}";

        var offset = (pagination.PageNumber - 1) * pagination.PageSize;
        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pagination.PageSize);

        var command = new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken);
        var countCommand = new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken);

        var items = (await connection.QueryAsync(command)).ToList();
        var totalCount = await connection.ExecuteScalarAsync<int>(countCommand);

        return (items, totalCount);
    }

    /// <summary>
    /// 单机台半小时槽位产能汇总查询（按日期和时间排序）
    /// </summary>
    public async Task<List<dynamic>> GetHourlyByDeviceAsync(
        Guid deviceId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();

        var sql = @"
            SELECT hc.date, hc.hour, hc.minute, hc.time_label, hc.shift_code, 
                   hc.total_count, hc.ok_count, hc.ng_count,
                   CASE WHEN hc.total_count > 0 
                        THEN ROUND(hc.ok_count * 100.0 / hc.total_count, 2) 
                        ELSE 0 END AS ok_rate,
                   d.device_name,
                   hc.reported_at
            FROM hourly_capacity hc
            INNER JOIN devices d ON hc.device_id = d.id
            WHERE hc.device_id = @DeviceId
              AND hc.date >= @StartDate
              AND hc.date <= @EndDate
            ORDER BY hc.date DESC, hc.hour DESC, hc.minute DESC";

        var command = new CommandDefinition(sql, new { DeviceId = deviceId, StartDate = startDate, EndDate = endDate }, cancellationToken: cancellationToken);

        return (await connection.QueryAsync(command)).ToList();
    }

    /// <summary>
    /// 获取设备最近一个月的半小时槽位产能（分页）
    /// </summary>
    public async Task<(List<dynamic> Items, int TotalCount)> GetHourlyLastMonthByDeviceAsync(
        Guid deviceId,
        Pagination pagination,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();

        var endDate = DateOnly.FromDateTime(DateTime.Now);
        var startDate = endDate.AddMonths(-1);

        var dataSql = @"
            SELECT hc.date, hc.hour, hc.minute, hc.time_label, hc.shift_code, 
                   hc.total_count, hc.ok_count, hc.ng_count,
                   CASE WHEN hc.total_count > 0 
                        THEN ROUND(hc.ok_count * 100.0 / hc.total_count, 2) 
                        ELSE 0 END AS ok_rate,
                   d.device_name,
                   hc.reported_at
            FROM hourly_capacity hc
            INNER JOIN devices d ON hc.device_id = d.id
            WHERE hc.device_id = @DeviceId
              AND hc.date >= @StartDate
              AND hc.date <= @EndDate
            ORDER BY hc.date DESC, hc.hour DESC, hc.minute DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var countSql = @"
            SELECT COUNT(*) 
            FROM hourly_capacity hc
            WHERE hc.device_id = @DeviceId
              AND hc.date >= @StartDate
              AND hc.date <= @EndDate";

        var offset = (pagination.PageNumber - 1) * pagination.PageSize;
        var parameters = new DynamicParameters(new { DeviceId = deviceId, StartDate = startDate, EndDate = endDate, Offset = offset, PageSize = pagination.PageSize });

        var items = (await connection.QueryAsync(new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken))).ToList();
        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        return (items, totalCount);
    }
}