namespace IIoT.Dapper.Production.QueryServices.PassStation;

public interface IPassStationQuerySql<TDto>
{
    string TableName { get; }

    string SelectColumns { get; }
}
