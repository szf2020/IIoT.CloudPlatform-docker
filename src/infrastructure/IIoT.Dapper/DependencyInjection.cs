using Dapper;
using IIoT.Dapper.QueryServices.Capacity;
using IIoT.Dapper.QueryServices.DeviceLog;
using IIoT.Dapper.QueryServices.PassStation;
using IIoT.Dapper.TypeHandlers;
using IIoT.Services.Common.Contracts.DapperQueries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IIoT.Dapper;

public static class DependencyInjection
{
    public static void AddDapper(this IHostApplicationBuilder builder)
    {
        // 注册自定义类型处理器（全局生效，只需调用一次）
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

        builder.Services.AddSingleton<IDbConnectionFactory>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connStr = config.GetConnectionString("iiot-db")
                ?? throw new InvalidOperationException("缺少 iiot-db 连接字符串");
            return new NpgsqlConnectionFactory(connStr);
        });

        builder.Services.AddScoped<IPassStationQueryService, PassStationQueryService>();
        builder.Services.AddScoped<ICapacityQueryService, CapacityQueryService>();
        builder.Services.AddScoped<IDeviceLogQueryService, DeviceLogQueryService>();
    }
}