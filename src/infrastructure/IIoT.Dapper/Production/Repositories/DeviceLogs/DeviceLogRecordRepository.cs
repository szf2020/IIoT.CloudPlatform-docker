using Dapper;
using IIoT.Core.Production.Contracts.RecordRepositories;

namespace IIoT.Dapper.Production.Repositories.DeviceLogs;

public sealed class DeviceLogRecordRepository(IDbConnectionFactory connectionFactory)
    : IDeviceLogRecordRepository
{
    public async Task InsertBatchAsync(
        IReadOnlyCollection<DeviceLogWriteModel> items,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0) return;

        const string sql = """
            insert into device_logs
            (
                id,
                device_id,
                level,
                message,
                log_time,
                received_at
            )
            values
            (
                @Id,
                @DeviceId,
                @Level,
                @Message,
                @LogTime,
                @ReceivedAt
            );
            """;

        using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(sql, items, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }
}
