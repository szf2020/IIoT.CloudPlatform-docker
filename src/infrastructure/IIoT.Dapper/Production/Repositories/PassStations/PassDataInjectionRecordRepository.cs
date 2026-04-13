using Dapper;
using IIoT.Core.Production.Contracts.RecordRepositories;

namespace IIoT.Dapper.Production.Repositories.PassStations;

public sealed class PassDataInjectionRecordRepository(IDbConnectionFactory connectionFactory)
    : IPassDataInjectionRecordRepository
{
    public async Task InsertBatchAsync(
        IReadOnlyCollection<PassDataInjectionWriteModel> items,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0) return;

        const string sql = """
            insert into pass_data_injection
            (
                id, device_id, cell_result,
                completed_time, received_at, barcode,
                pre_injection_time, pre_injection_weight,
                post_injection_time, post_injection_weight, injection_volume
            )
            values
            (
                @Id, @DeviceId, @CellResult,
                @CompletedTime, @ReceivedAt, @Barcode,
                @PreInjectionTime, @PreInjectionWeight,
                @PostInjectionTime, @PostInjectionWeight, @InjectionVolume
            )
            on conflict (device_id, barcode, completed_time) do nothing;
            """;

        using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(sql, items, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }
}
