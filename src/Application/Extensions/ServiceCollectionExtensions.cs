using Microsoft.Extensions.DependencyInjection;

namespace Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        //services.AddHybridCache(options =>
        //{
        //    options.MaximumPayloadBytes = 5 * 1024 * 1024; // 5 MB
        //    options.MaximumKeyLength = 512;
        //    options.DefaultEntryOptions = new HybridCacheEntryOptions
        //    {
        //        Expiration = TimeSpan.FromMinutes(5),
        //        LocalCacheExpiration = TimeSpan.FromMinutes(5),
        //        Flags = HybridCacheEntryFlags.DisableDistributedCache
        //    };
        //});

        return services;
    }
}