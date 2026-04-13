using System.Reflection;
using IIoT.Services.Common.Contracts;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IIoT.EventBus;

public static class DependencyInjection
{
    public static void AddEventBus(this IHostApplicationBuilder builder, params Assembly[] assemblies)
    {
        builder.Services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

        builder.Services.AddMassTransit(x =>
        {
            if (assemblies.Length > 0)
            {
                x.AddConsumers(assemblies);
            }

            x.SetKebabCaseEndpointNameFormatter();

            x.UsingRabbitMq((context, cfg) =>
            {
                var connectionString = builder.Configuration.GetConnectionString("eventbus");
                if (!string.IsNullOrEmpty(connectionString))
                {
                    cfg.Host(connectionString);
                }

                cfg.UseMessageRetry(r => r.Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)));
                cfg.ConfigureEndpoints(context);
            });
        });

        // 不阻塞应用启动，避免 RabbitMQ 尚未就绪时卡住宿主。
        builder.Services.Configure<MassTransitHostOptions>(options =>
        {
            options.WaitUntilStarted = false;
            options.StartTimeout = TimeSpan.FromSeconds(30);
            options.StopTimeout = TimeSpan.FromSeconds(30);
        });
    }
}
