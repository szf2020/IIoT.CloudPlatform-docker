using IIoT.Infrastructure.Authentication;
using IIoT.Services.Common.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using IIoT.Infrastructure.EntityFrameworkCore;
using System.Collections.Generic;
using System.Text;

namespace IIoT.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructures(this IHostApplicationBuilder builder)
    {
        builder.AddEfCore();
        // 绑定配置
        builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

        // 注册 JWT 生成器 (单例即可，因为它只是个纯函数计算器)
        builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
    }
}