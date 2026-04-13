using Dapper;
using IIoT.Services.Common.Contracts.DapperQueries;
using IIoT.SharedKernel.Paging;

namespace IIoT.Dapper.Production.QueryServices.PassStation;

public class PassStationQueryService(IDbConnectionFactory connectionFactory) : IPassStationQueryService
{
    public async Task<(List<InjectionPassListItemDto> Items, int TotalCount)> GetInjectionByConditionAsync(
        Pagination pagination,
        List<Guid>? deviceIds = null,
        Guid? deviceId = null,
        string? barcode = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();

        var conditions = "WHERE 1=1";
        var parameters = new DynamicParameters();

        if (deviceIds != null && deviceIds.Count > 0)
        {
            conditions += " AND device_id = ANY(@DeviceIds)";
            parameters.Add("DeviceIds", deviceIds.ToArray());
        }

        if (deviceId.HasValue)
        {
            conditions += " AND device_id = @DeviceId";
            parameters.Add("DeviceId", deviceId.Value);
        }

        if (!string.IsNullOrWhiteSpace(barcode))
        {
            conditions += " AND barcode = @Barcode";
            parameters.Add("Barcode", barcode);
        }

        if (startTime.HasValue)
        {
            conditions += " AND completed_time >= @StartTime";
            parameters.Add("StartTime", startTime.Value);
        }

        if (endTime.HasValue)
        {
            conditions += " AND completed_time <= @EndTime";
            parameters.Add("EndTime", endTime.Value);
        }

        var dataSql = $@"
            SELECT id                   AS Id,
                   device_id            AS DeviceId,
                   barcode              AS Barcode,
                   cell_result          AS CellResult,
                   pre_injection_time   AS PreInjectionTime,
                   pre_injection_weight AS PreInjectionWeight,
                   post_injection_time  AS PostInjectionTime,
                   post_injection_weight AS PostInjectionWeight,
                   injection_volume     AS InjectionVolume,
                   completed_time       AS CompletedTime,
                   received_at          AS ReceivedAt
            FROM pass_data_injection
            {conditions}
            ORDER BY completed_time DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var countSql = $"SELECT COUNT(*) FROM pass_data_injection {conditions}";

        var offset = (pagination.PageNumber - 1) * pagination.PageSize;
        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pagination.PageSize);

        var command = new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken);
        var countCommand = new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken);

        var items = (await connection.QueryAsync<InjectionPassListItemDto>(command)).ToList();
        var totalCount = await connection.ExecuteScalarAsync<int>(countCommand);

        return (items, totalCount);
    }

    public async Task<InjectionPassDetailDto?> GetInjectionDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();

        const string sql = @"
            SELECT id                   AS Id,
                   device_id            AS DeviceId,
                   cell_result          AS CellResult,
                   completed_time       AS CompletedTime,
                   received_at          AS ReceivedAt,
                   barcode              AS Barcode,
                   pre_injection_time   AS PreInjectionTime,
                   pre_injection_weight AS PreInjectionWeight,
                   post_injection_time  AS PostInjectionTime,
                   post_injection_weight AS PostInjectionWeight,
                   injection_volume     AS InjectionVolume
            FROM pass_data_injection
            WHERE id = @Id";
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<InjectionPassDetailDto>(command);
    }

    public async Task<(List<InjectionPassListItemDto> Items, int TotalCount)> GetInjectionLatest200ByDeviceAsync(
        Guid deviceId,
        Pagination pagination,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();

        var dataSql = @"
            WITH latest AS (
                SELECT id                   AS Id,
                       device_id            AS DeviceId,
                       barcode              AS Barcode,
                       cell_result          AS CellResult,
                       pre_injection_time   AS PreInjectionTime,
                       pre_injection_weight AS PreInjectionWeight,
                       post_injection_time  AS PostInjectionTime,
                       post_injection_weight AS PostInjectionWeight,
                       injection_volume     AS InjectionVolume,
                       completed_time       AS CompletedTime,
                       received_at          AS ReceivedAt,
                       ROW_NUMBER() OVER (ORDER BY completed_time DESC) AS rn
                FROM pass_data_injection
                WHERE device_id = @DeviceId
            )
            SELECT Id, DeviceId, Barcode, CellResult,
                   PreInjectionTime, PreInjectionWeight,
                   PostInjectionTime, PostInjectionWeight,
                   InjectionVolume, CompletedTime, ReceivedAt
            FROM latest
            WHERE rn <= 200
            ORDER BY CompletedTime DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var countSql = @"
            SELECT LEAST(COUNT(*), 200)
            FROM pass_data_injection
            WHERE device_id = @DeviceId";

        var offset = (pagination.PageNumber - 1) * pagination.PageSize;
        var parameters = new { DeviceId = deviceId, Offset = offset, PageSize = pagination.PageSize };

        var command = new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken);
        var countCommand = new CommandDefinition(countSql, new { DeviceId = deviceId }, cancellationToken: cancellationToken);

        var items = (await connection.QueryAsync<InjectionPassListItemDto>(command)).ToList();
        var totalCount = await connection.ExecuteScalarAsync<int>(countCommand);

        return (items, totalCount);
    }
}
