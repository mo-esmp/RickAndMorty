using Microsoft.Extensions.Caching.Distributed;

namespace Application.Common;

public interface IAppCache : IDistributedCache
{
    Task<T?> GetAsync<T>(string key, CancellationToken token = default);

    Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions? options = null, CancellationToken token = default);
}