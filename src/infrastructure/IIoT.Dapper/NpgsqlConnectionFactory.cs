using System.Data;
using Npgsql;

namespace IIoT.Dapper;

public class NpgsqlConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(connectionString);
    }
}