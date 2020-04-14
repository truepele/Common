using System;
using System.Threading;
using System.Threading.Tasks;

namespace Caching
{
    public interface ICache
    {
        object Get(string key, Type type);
        
        TValue Get<TValue>(string key);
        
        Task<TValue> GetAsync<TValue>(string key, CancellationToken cancellation = default);
        
        TValue GetOrCreate<TValue>(string key, CacheEntryOptions options, Func<string, TValue> factory);
        
        object GetOrCreate(string key, Type type, CacheEntryOptions options, Func<string, object> factory);
        
        Task<TValue> GetOrCreateAsync<TValue>(string key,
            CacheEntryOptions options,
            Func<string, Task<TValue>> factory, 
            CancellationToken cancellation = default);
        
        void Set<TValue>(string key, TValue value, CacheEntryOptions options = null);

        Task SetAsync<TValue>(string key,
            TValue value,
            CacheEntryOptions options = null,
            CancellationToken cancellation = default);
    }
}