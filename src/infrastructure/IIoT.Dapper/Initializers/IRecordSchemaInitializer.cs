namespace IIoT.Dapper.Initializers;

public interface IRecordSchemaInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
