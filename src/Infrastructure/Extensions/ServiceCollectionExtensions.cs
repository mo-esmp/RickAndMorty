using Application.Common;
using Domain.Characters;
using Infrastructure.Cache;
using Infrastructure.DataPersistence;
using Infrastructure.DataPersistence.Interceptors;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        if (isDevelopment)
        {
            services.AddDbContext<ApplicationDbContext, SqlLiteDbContext>((serviceProvider, options) =>
            {
                options.AddInterceptors(serviceProvider.GetRequiredService<ISaveChangesInterceptor>());
            });
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddDbContext<ApplicationDbContext, PostgreSqlDbContext>((serviceProvider, options) =>
            {
                options.AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>());
            });

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetConnectionString("RedisConnection");
                options.InstanceName = "TestInstance";
            });
        }

        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
        services.AddScoped<ICharacterRepository, CharacterRepository>();

        services.Configure<AppCacheOptions>(configuration.GetSection("AppCache"));
        services.AddSingleton<IAppCache, DistributedAppCache>();

        return services;
    }
}