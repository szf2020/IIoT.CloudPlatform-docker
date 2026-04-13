using IIoT.Dapper.Bootstrap;
using IIoT.DataWorker.Consumers;
using IIoT.Infrastructure;
using IIoT.Infrastructure.Logging;
using IIoT.ProductionService.Commands.Capacities;
using IIoT.Services.Common.Behaviors;
using MassTransit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

// DataWorker 只消费三个 Persist Handler,但 MediatR 会扫描整个
// ProductionService 程序集的所有 Handler。那些用不到的 Handler 依赖
// EF / AutoMapper / ICurrentUser 等 DataWorker 不需要的服务。
// 关闭构建期严格校验,让它们保持"已注册但永不实例化"状态 —
// 实际运行时三个 Consumer 只会 Send 三个 PersistXxxCommand,
// 对应的 Persist Handler 依赖(IDeviceIdentityQueryService +
// IXxxRecordRepository)在本文件已全部注册,不会触发解析错误。
builder.ConfigureContainer(
    new DefaultServiceProviderFactory(new ServiceProviderOptions
    {
        ValidateOnBuild = false,
        ValidateScopes = false
    }));

// 1. Serilog 日志
builder.AddSerilog("dataworker");

// 2. Aspire 服务默认配置
builder.AddServiceDefaults();

// 3. Dapper(记录类写入与查询的唯一持久化通道)
builder.AddDapper();

// 4. 基础设施(FusionCache L1+L2+Backplane + ICacheService + IDistributedLockService)
builder.AddInfrastructures();

// 5. MediatR — 从 ProductionService 程序集扫描所有 Persist 用例 Handler
builder.Services.AddMediatR(cfg =>
{
    cfg.LicenseKey = "eyJhbGciOiJSUzI1NiIsImtpZCI6Ikx1Y2t5UGVubnlTb2Z0d2FyZUxpY2Vuc2VLZXkvYmJiMTNhY2I1OTkwNGQ4OWI0Y2IxYzg1ZjA4OGNjZjkiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwczovL2x1Y2t5cGVubnlzb2Z0d2FyZS5jb20iLCJhdWQiOiJMdWNreVBlbm55U29mdHdhcmUiLCJleHAiOiIxODA0MTE4NDAwIiwiaWF0IjoiMTc3MjYwOTI4MCIsImFjY291bnRfaWQiOiIwMTljYjdiYTA0NGM3Y2FjYTcyZDNhMWQ3YjRlYzZjNiIsImN1c3RvbWVyX2lkIjoiY3RtXzAxa2p2dnk2ZjFjdjM1bmF3NzNmNGZ0MTE4Iiwic3ViX2lkIjoiLSIsImVkaXRpb24iOiIwIiwidHlwZSI6IjIifQ.vuSHJIt34rSumtJdD5ZI6gorKQmaD5Msk28ucJr2GIPFR1TsOqtyvdMydzyN5nFIEv_EeGNOu_LfTHTCDz2G-Vu9atS1h7xhIoQqNT8PvuLPHEHrf90YjOKEe4rxjohth1fC2SqpkvrJ0VzEPWQNsy5lvoLOZmzw2WAHa6NBy5bc4R9tQNwOUUbxLSwhmnyOo6K1Td87CBXEjAveGrXuSwhNE0NnQWuTs1ptcK40tfkq3T3Bigh2NO-QDiGuipxoS5AQIkO6n-wLjuhFW1078IEeyh9wct2l7s8htWNQLIlmRvFFJPiN2m1-cI60ds4SYfr4FA4pM6DSNXIMDMeGyA";
    cfg.RegisterServicesFromAssemblyContaining<ReceiveHourlyCapacityCommand>();
    cfg.AddOpenBehavior(typeof(DistributedLockBehavior<,>));
});

// 6. MassTransit + RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<PassDataInjectionConsumer>(cfg => { cfg.ConcurrentMessageLimit = 4; });
    x.AddConsumer<DeviceLogConsumer>(cfg => { cfg.ConcurrentMessageLimit = 3; });
    x.AddConsumer<HourlyCapacityConsumer>(cfg => { cfg.ConcurrentMessageLimit = 1; });

    x.UsingRabbitMq((context, cfg) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("eventbus");
        cfg.Host(connectionString);

        cfg.UseMessageRetry(r => r.Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)));

        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();