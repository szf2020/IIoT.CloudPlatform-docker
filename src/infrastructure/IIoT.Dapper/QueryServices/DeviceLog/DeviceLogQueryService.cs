using Dapper;
using IIoT.Dapper;
using IIoT.Services.Common.Contracts.DapperQueries;
using IIoT.SharedKernel.Paging;

namespace IIoT.Dapper.QueryServices.DeviceLog;

public class DeviceLogQueryService(IDbConnectionFactory connectionFactory) : IDeviceLogQueryService
{
    public async Task<(List<dynamic> Items, int TotalCount)> GetPagedAsync(
        Pagination pagination,
        Guid? deviceId = null,
        string? level = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
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

        if (!string.IsNullOrWhiteSpace(level))
        {
            conditions += " AND level = @Level";
            parameters.Add("Level", level);
        }

        if (startTime.HasValue)
        {
            conditions += " AND log_time >= @StartTime";
            parameters.Add("StartTime", startTime.Value);
        }

        if (endTime.HasValue)
        {
            conditions += " AND log_time <= @EndTime";
            parameters.Add("EndTime", endTime.Value);
        }

        var dataSql = $@"
            SELECT id, device_id, level, message, log_time, received_at
            FROM device_logs
            {conditions}
            ORDER BY log_time DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var countSql = $"SELECT COUNT(*) FROM device_logs {conditions}";

        var offset = (pagination.PageNumber - 1) * pagination.PageSize;
        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pagination.PageSize);

        var command = new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken);
        var countCommand = new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken);

        var items = (await connection.QueryAsync(command)).ToList();
        var totalCount = await connection.ExecuteScalarAsync<int>(countCommand);

        return (items, totalCount);
    }
}