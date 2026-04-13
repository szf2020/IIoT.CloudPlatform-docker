namespace IIoT.Core.Production.Contracts.PassStation;

public interface IPassStationWriteModel;

public interface IPassStationRepository<TWriteModel>
    where TWriteModel : IPassStationWriteModel
{
    Task InsertBatchAsync(
        IReadOnlyCollection<TWriteModel> items,
        CancellationToken cancellationToken = default);
}
