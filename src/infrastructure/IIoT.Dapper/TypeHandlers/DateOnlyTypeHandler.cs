using Dapper;
using System.Data;

namespace IIoT.Dapper.TypeHandlers;

/// <summary>
/// Dapper 自定义类型处理器：支持 DateOnly ↔ PostgreSQL date 列的互转
/// </summary>
public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }

    public override DateOnly Parse(object value)
    {
        return value switch
        {
            DateTime dt => DateOnly.FromDateTime(dt),
            DateOnly d => d,
            _ => DateOnly.FromDateTime(Convert.ToDateTime(value))
        };
    }
}
