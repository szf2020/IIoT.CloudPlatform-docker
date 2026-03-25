using IIoT.Dapper.QueryServices.Capacity;
using IIoT.Dapper.QueryServices.DeviceLog;
using IIoT.Dapper.QueryServices.PassStation;
using IIoT.Services.Common.Contracts.DapperQueries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IIoT.Dapper;

public static class DependencyInjection
{
    public static void AddDapper(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("iiot-db")
            ?? throw new InvalidOperationException("未找到数据库连接字符串 'iiot-db'");

        // 连接工厂
        builder.Services.AddSingleton<IDbConnectionFactory>(new NpgsqlConnectionFactory(connectionString));

        // 查询服务
        builder.Services.AddScoped<IPassStationQueryService, PassStationQueryService>();
        builder.Services.AddScoped<ICapacityQueryService, CapacityQueryService>();
        builder.Services.AddScoped<IDeviceLogQueryService, DeviceLogQueryService>();
    }
}