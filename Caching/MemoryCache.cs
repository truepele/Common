using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Caching
{
    public class MemoryCache : ICache
    {
        private readonly IMemoryCache _memoryCache;
        

        public MemoryCache(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }
        
        
        public object Get(string key, Type type)
        {
            if (!_memoryCache.TryGetValue(key, out var value))
            {
                return null;
            }

            if (value.GetType() != type)
            {
                throw new InvalidOperationException();
            }

            return value;
        }

        public TValue Get<TValue>(string key)
        {
            return _memoryCache.Get<TValue>(key);
        }

        public Task<TValue> GetAsync<TValue>(string key, CancellationToken cancellation = default)
        {
            return Task.FromResult(_memoryCache.Get<TValue>(key));
        }

        public TValue GetOrCreate<TValue>(string key, CacheEntryOptions options, Func<string, TValue> factory)
        {
            if (_memoryCache.TryGetValue<TValue>(key, out var value))
            {
                return value;
            }

            value = factory(key);
            Set(key, value, options);

            return value;
        }

        public object GetOrCreate(string key, Type type, CacheEntryOptions options, Func<string, object> factory)
        {
            if (_memoryCache.TryGetValue(key, out var value))
            {
                return value;
            }

            value = factory(key);
            Set(key, value, options);

            return value;
        }

        public async Task<TValue> GetOrCreateAsync<TValue>(string key, CacheEntryOptions options, Func<string, Task<TValue>> factory,
            CancellationToken cancellation = default)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            
            if (_memoryCache.TryGetValue<TValue>(key, out var value))
            {
                return value;
            }

            value = await factory(key).ConfigureAwait(false);
            await SetAsync(key, value, options, cancellation).ConfigureAwait(false);

            return value;
        }

        public void Set<TValue>(string key, TValue value, CacheEntryOptions options = null)
        {
            if (options == null || options.Type == ExpirationType.NoExpiration)
            {
                _memoryCache.Set(key, value);
            }
            else
            {
                _memoryCache.Set(key, value, options.ToMemoryCacheEntryOptions());
            }
        }

        public Task SetAsync<TValue>(string key, TValue value, CacheEntryOptions options = null, CancellationToken cancellation = default)
        {
            if (options == null || options.Type == ExpirationType.NoExpiration)
            {
                _memoryCache.Set(key, value);
            }
            else
            {
                _memoryCache.Set(key, value, options.ToMemoryCacheEntryOptions());
            }
            
            return Task.CompletedTask;
        }
    }
}