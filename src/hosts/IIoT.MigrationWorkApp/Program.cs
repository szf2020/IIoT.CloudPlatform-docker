using IIoT.EntityFrameworkCore;
using IIoT.Infrastructure;
using IIoT.MigrationWorkApp;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

builder.AddEfCore();
builder.AddInfrastructures();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

var host = builder.Build();
host.Run();