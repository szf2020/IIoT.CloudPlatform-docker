var builder = DistributedApplication.CreateBuilder(args);

// 1. 甩掉有毒的 AddRedis 插件！用基础容器拉取干净的 Redis。
// 【注意】这里绝对不写死宿主机端口！只暴露容器内部的 6379，让系统去动态分配！
var redis = builder.AddContainer("redis-cache", "redis", "7.4-alpine")
    .WithEndpoint(targetPort: 6379, name: "redis-endpoint");

var password = builder.AddParameter("pg-password", secret: true);

var postgres = builder.AddPostgres("postgres", password: password)
    .WithDataVolume("postgres-iiot")
    .WithPgWeb()
    .AddDatabase("iiot-db");

var migration = builder.AddProject<Projects.IIoT_MigrationWorkApp>("iiot-migrationworkapp")
    .WithReference(postgres)
    .WaitFor(postgres)
    // 2. 云原生魔法：使用 .GetEndpoint()，Aspire 会自动把安全的动态端口塞过去
    .WithReference(redis.GetEndpoint("redis-endpoint"));

builder.AddProject<Projects.IIoT_HttpApi>("iiot-httpapi")
    .WithReference(postgres)
    // 3. API 同样自动拿到动态连接，不用你手动去配置任何 appsettings！
    .WithReference(redis.GetEndpoint("redis-endpoint"))
    .WaitFor(migration);

builder.Build().Run();