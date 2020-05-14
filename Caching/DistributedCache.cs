using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Caching
{
    public class DistributedCache : ICache
    {
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<DistributedCache> _logger;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        
        
        public DistributedCache(IDistributedCache distributedCache,
            ILogger<DistributedCache> logger,
            JsonSerializerSettings jsonSerializerSettings = null)
        {
            _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonSerializerSettings = jsonSerializerSettings ?? new JsonSerializerSettings();
        }
        

        public object Get(string key, Type type)
        {
            var strValue = _distributedCache.GetString(key);
            return string.IsNullOrEmpty(strValue) 
                ? default 
                : JsonConvert.DeserializeObject(strValue, type, _jsonSerializerSettings);
        }

        public TValue Get<TValue>(string key)
        {
            var strValue = _distributedCache.GetString(key);
            return string.IsNullOrEmpty(strValue) 
                ? default 
                : JsonConvert.DeserializeObject<TValue>(strValue, _jsonSerializerSettings);
        }

        public async Task<TValue> GetAsync<TValue>(string key, CancellationToken cancellation = default)
        {
            var strValue = await _distributedCache.GetStringAsync(key, token: cancellation).ConfigureAwait(false);
            return string.IsNullOrEmpty(strValue) 
                ? default 
                : JsonConvert.DeserializeObject<TValue>(strValue, _jsonSerializerSettings);
        }
        
        public bool GetIfCached<TValue>(string key, out TValue result)
        {
            var strValue = _distributedCache.GetString(key);
            result = default;

            if (string.IsNullOrEmpty(strValue))
            {
                return false;
            }

            result = JsonConvert.DeserializeObject<TValue>(strValue, _jsonSerializerSettings);
            return true;
        }

        public async Task<(bool wasInCache, TValue value)> GetIfCachedAsync<TValue>(string key, CancellationToken cancellation = default)
        {
            var strValue = await _distributedCache.GetStringAsync(key, token: cancellation).ConfigureAwait(false);
            
            if (string.IsNullOrEmpty(strValue))
            {
                return (false, default);
            }

            var value = JsonConvert.DeserializeObject<TValue>(strValue, _jsonSerializerSettings);
            return (true, value);
        }


        public TValue GetOrCreate<TValue>(string key, CacheEntryOptions options, Func<string, TValue> factory)
        {
            if (GetIfCached(key, out TValue value))
            {
                return value;
            }

            value = factory(key);
            Set(key, value, options);

            return value;
        }

        public object GetOrCreate(string key, Type type, CacheEntryOptions options, Func<string, object> factory)
        {
            var strValue = _distributedCache.GetString(key);
            if (!string.IsNullOrEmpty(strValue))
            {
                return JsonConvert.DeserializeObject(strValue, type, _jsonSerializerSettings);
            }

            var value = factory(key);
            Set(key, value, options);

            return value;
        }

        public async Task<TValue> GetOrCreateAsync<TValue>(string key,
            CacheEntryOptions options,
            Func<string, Task<TValue>> factory,
            CancellationToken cancellation = default)
        {
            (bool wasInCache, TValue value) = await GetIfCachedAsync<TValue>(key, cancellation).ConfigureAwait(false);
            if (wasInCache)
            {
                return value;
            }

            value = await factory(key).ConfigureAwait(false);
            await SetAsync(key, value, options, cancellation).ConfigureAwait(false);

            return value;
        }

        public void Set<TValue>(string key, TValue value, CacheEntryOptions options = null)
        { 
            var strValue = JsonConvert.SerializeObject(value, _jsonSerializerSettings);

            if (options == null || options.Type == ExpirationType.NoExpiration)
            {
                _distributedCache.SetString(key, strValue);
            }
            else
            {
                _distributedCache.SetString(key, strValue, MapCacheEntryOptions(options));
            }
        }

        public Task SetAsync<TValue>(string key, TValue value, CacheEntryOptions options = null, CancellationToken cancellation = default)
        {
            var strValue = JsonConvert.SerializeObject(value, _jsonSerializerSettings);
            return options.Type == ExpirationType.NoExpiration 
                ? _distributedCache.SetStringAsync(key, strValue, cancellation) 
                : _distributedCache.SetStringAsync(key, strValue, MapCacheEntryOptions(options), cancellation);
        }

        private static DistributedCacheEntryOptions MapCacheEntryOptions(CacheEntryOptions options)
        {
            var distributedCacheOptions = new DistributedCacheEntryOptions();
            switch (options.Type)
            {
                case ExpirationType.NoExpiration:
                    distributedCacheOptions.SetAbsoluteExpiration(TimeSpan.MinValue);
                    break;
                case ExpirationType.Relative:
                    distributedCacheOptions.SetAbsoluteExpiration(options.Ttl);
                    break;
                case ExpirationType.Sliding:
                    distributedCacheOptions.SetSlidingExpiration(options.Ttl);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return distributedCacheOptions;
        }
    }
}
