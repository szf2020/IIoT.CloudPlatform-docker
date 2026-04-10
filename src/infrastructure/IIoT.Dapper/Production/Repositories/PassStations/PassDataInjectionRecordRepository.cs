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
                id, device_id, mac_address, client_code, cell_result,
                completed_time, received_at, barcode,
                pre_injection_time, pre_injection_weight,
                post_injection_time, post_injection_weight, injection_volume
            )
            values
            (
                @Id, @DeviceId, @MacAddress, @ClientCode, @CellResult,
                @CompletedTime, @ReceivedAt, @Barcode,
                @PreInjectionTime, @PreInjectionWeight,
                @PostInjectionTime, @PostInjectionWeight, @InjectionVolume
            )
            on conflict (device_id, barcode, completed_time) do nothing;
            """;

        var rows = items.Select(x => new
        {
            x.Id,
            x.DeviceId,
            MacAddress = x.Instance.MacAddress,
            ClientCode = x.Instance.ClientCode,
            x.CellResult,
            x.CompletedTime,
            x.ReceivedAt,
            x.Barcode,
            x.PreInjectionTime,
            x.PreInjectionWeight,
            x.PostInjectionTime,
            x.PostInjectionWeight,
            x.InjectionVolume
        });

        using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(sql, rows, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }
}