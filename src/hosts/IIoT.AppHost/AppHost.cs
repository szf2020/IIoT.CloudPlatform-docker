using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

// 🌟 1. 终极修复：使用我们自定义的纯净 Redis 资源！
// 它没有任何 Aspire 官方的 TLS 强行加戏，容器只在 6379 极速裸奔启动！
var redis = builder.AddResource(new CleanRedisResource("redis-cache"))
                   .WithImage("redis", "7.4-alpine")
                   .WithEndpoint(targetPort: 6379, name: "tcp");

var password = builder.AddParameter("pg-password", secret: true);

var postgres = builder.AddPostgres("postgres", password: password)
    .WithDataVolume("postgres-iiot")
    .WithPgWeb()
    .AddDatabase("iiot-db");

var migration = builder.AddProject<Projects.IIoT_MigrationWorkApp>("iiot-migrationworkapp")
    .WithReference(postgres)
    .WaitFor(postgres)
    // 🌟 2. 直接引用，它会自动生成完美的 host:port 字符串！
    .WithReference(redis);

builder.AddProject<Projects.IIoT_HttpApi>("iiot-httpapi")
    .WithReference(postgres)
    // 🌟 3. 后端啥也不用改，AddRedisDistributedCache 瞬间成功接管！
    .WithReference(redis)
    .WaitFor(migration);

builder.Build().Run();

// =========================================================================
// 🌟 魔法黑科技：自定义一个纯净版 Redis 资源类
// =========================================================================
internal class CleanRedisResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    // 核心精髓：精准剥离出动态分配的 IP 和端口，强行组装成 "host:port" 格式，彻底踢掉导致报错的 "tcp://" 前缀！
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{this.GetEndpoint("tcp").Property(EndpointProperty.Host)}:{this.GetEndpoint("tcp").Property(EndpointProperty.Port)}");
}