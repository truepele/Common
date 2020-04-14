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
            var strValue = await _distributedCache.GetStringAsync(key, token: cancellation);

            return string.IsNullOrEmpty(strValue) 
                ? default 
                : JsonConvert.DeserializeObject<TValue>(strValue, _jsonSerializerSettings);
        }

        public TValue GetOrCreate<TValue>(string key, CacheEntryOptions options, Func<string, TValue> factory)
        {
            try
            {
                var strValue = _distributedCache.GetString(key);
                if (!string.IsNullOrEmpty(strValue))
                {
                    return JsonConvert.DeserializeObject<TValue>(strValue, _jsonSerializerSettings);
                }
            }
            catch (JsonException e)
            {
                _logger.LogError(e, 
                    $"Failed on deserialization of string from distributed cache. Key: {key}, Expected Type: {typeof(TValue)} ");
            }

            var value = factory(key);
            Set(key, value, options);

            return value;
        }

        public object GetOrCreate(string key, Type type, CacheEntryOptions options, Func<string, object> factory)
        {
            try
            {
                var strValue = _distributedCache.GetString(key);
                if (!string.IsNullOrEmpty(strValue))
                {
                    return JsonConvert.DeserializeObject(strValue, type, _jsonSerializerSettings);
                }
            }
            catch (JsonException e)
            {
                _logger.LogError(e, 
                    $"Failed on deserialization of string from distributed cache. Key: {key}, Expected Type: {type} ");
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
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            try
            {
                var strValue = await _distributedCache.GetStringAsync(key, cancellation);
                if (!string.IsNullOrEmpty(strValue))
                {
                    return JsonConvert.DeserializeObject<TValue>(strValue, _jsonSerializerSettings);
                }
            }
            catch (JsonException e)
            {
                _logger.LogError(e, 
                    $"Failed on deserialization of string from distributed cache. Key: {key}, Expected Type: {typeof(TValue)} ");
            }

            var value = await factory(key).ConfigureAwait(false);
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
                _distributedCache.SetString(key, strValue, options.ToDistributedCacheEntryOptions());
            }
        }

        public Task SetAsync<TValue>(string key, TValue value, CacheEntryOptions options = null, CancellationToken cancellation = default)
        {
            var strValue = JsonConvert.SerializeObject(value, _jsonSerializerSettings);

            return options.Type == ExpirationType.NoExpiration 
                ? _distributedCache.SetStringAsync(key, strValue, cancellation) 
                : _distributedCache.SetStringAsync(key, strValue, options.ToDistributedCacheEntryOptions(), cancellation);
        }
    }
}
