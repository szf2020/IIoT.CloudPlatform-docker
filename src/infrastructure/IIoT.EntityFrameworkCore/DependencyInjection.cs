using IIoT.EntityFrameworkCore.Identity;
using IIoT.EntityFrameworkCore.Persistence;
using IIoT.EntityFrameworkCore.Repository;
using IIoT.Services.Common.Caching.Options;
using IIoT.Services.Common.Contracts;
using IIoT.Services.Common.Contracts.Authorization;
using IIoT.Services.Common.Contracts.RecordQueries;
using IIoT.SharedKernel.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IIoT.EntityFrameworkCore;

public static class DependencyInjection
{
    public static void AddEfCore(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<IIoTDbContext>("iiot-db");

        builder.Services.Configure<PermissionCacheOptions>(
            builder.Configuration.GetSection("PermissionCache"));
        builder.Services.AddScoped<IPermissionProvider, PermissionProvider>();

        builder.Services.AddScoped(typeof(IReadRepository<>), typeof(EfReadRepository<>));
        builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));

        builder.Services.AddScoped<IIdentityAccountStore, IdentityAccountStore>();
        builder.Services.AddScoped<IEmployeeLookupService, EmployeeLookupService>();
        builder.Services.AddScoped<IDevicePermissionService, DevicePermissionService>();
        builder.Services.AddScoped<IIdentityPasswordService, IdentityPasswordService>();
        builder.Services.AddScoped<IRolePolicyService, RolePolicyService>();
        builder.Services.AddScoped<IUserQueryService, UserQueryService>();
        builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        builder.Services.AddScoped<IProcessReadQueryService, QueryServices.ProcessReadQueryService>();
        builder.Services.AddScoped<IDeviceReadQueryService, QueryServices.DeviceReadQueryService>();
        builder.Services.AddScoped<IRecipeReadQueryService, QueryServices.RecipeReadQueryService>();

        builder.Services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
        })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<IIoTDbContext>();
    }
}
