using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Caching
{
    public class ExceptionHandlingCacheDecorator : ICache
    {
        private readonly ICache _cacheImplementation;
        private readonly ILogger<ExceptionHandlingCacheDecorator> _logger;
        

        public ExceptionHandlingCacheDecorator(ICache cacheImplementation, ILogger<ExceptionHandlingCacheDecorator> logger)
        {
            _cacheImplementation = cacheImplementation ?? throw new ArgumentNullException(nameof(cacheImplementation));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        

        public object Get(string key, Type type)
        {
            try
            {
                return _cacheImplementation.Get(key, type);
            }
            catch (Exception e)
            {
                LogError(e, nameof(Get), type, key);
                return null;
            }
        }

        public TValue Get<TValue>(string key)
        {
            try
            {
                return _cacheImplementation.Get<TValue>(key);
            }
            catch (Exception e)
            {
                LogError<TValue>(e, nameof(Get), key);
                return default;
            }
        }

        public bool GetIfCached<TValue>(string key, out TValue value)
        {
            try
            {
                return _cacheImplementation.GetIfCached(key, out value);
            }
            catch (Exception e)
            {
                LogError<TValue>(e, nameof(GetIfCachedAsync), key);
            }

            value = default;
            return false;
        }

        public async Task<(bool wasInCache, TValue value)> GetIfCachedAsync<TValue>(string key, CancellationToken cancellation = default)
        {
            try
            {
                return await _cacheImplementation.GetIfCachedAsync<TValue>(key, cancellation);
            }
            catch (Exception e)
            {
                LogError<TValue>(e, nameof(GetIfCachedAsync), key);
                return default;
            }
        }

        public async Task<TValue> GetAsync<TValue>(string key, CancellationToken cancellation = default)
        {
            try
            {
                return await _cacheImplementation.GetAsync<TValue>(key, cancellation);
            }
            catch (Exception e)
            {
                LogError<TValue>(e, nameof(GetAsync), key);
                return default;
            }
        }

        public TValue GetOrCreate<TValue>(string key, CacheEntryOptions options, Func<string, TValue> factory)
        {
            if (GetIfCached<TValue>(key, out var value))
            {
                return value;
            }
            
            var result = factory(key);

            if (result != null)
            {
                Set(key, result, options);
            }

            return result;
        }

        public object GetOrCreate(string key, Type type, CacheEntryOptions options, Func<string, object> factory)
        {
            object result = Get(key, type);

            if (result != null)
            {
                return result;
            }

            result = factory(key);

            if (result != null)
            {
                Set(key, result, options);
            }

            return result;
        }

        public async Task<TValue> GetOrCreateAsync<TValue>(string key, CacheEntryOptions options, Func<string, Task<TValue>> factory, CancellationToken cancellation = default)
        {
            (bool wasInCache, TValue value) = await GetIfCachedAsync<TValue>(key, cancellation);

            if (wasInCache)
            {
                return value;
            }

            value = await factory(key); 
            await SetAsync(key, value, options, cancellation);

            return value;
        }

        public void Set<TValue>(string key, TValue value, CacheEntryOptions options = null)
        {
            try
            {
                _cacheImplementation.Set(key, value, options);
            }
            catch (Exception e)
            {
                LogError<TValue>(e, nameof(Set), key);
            }
        }

        public async Task SetAsync<TValue>(string key, TValue value, CacheEntryOptions options = null, CancellationToken cancellation = default)
        {
            try
            {
                await _cacheImplementation.SetAsync(key, value, options, cancellation);
            }
            catch (Exception e)
            {
                LogError<TValue>(e, nameof(SetAsync), key);
            }
        }


        private void LogError<TValue>(Exception exception, string methodName, string key)
        {
            _logger.LogError(exception,
                $"Exception occured in the inner {methodName} for key '{key}' and generic argument '{typeof(TValue)}'");
        }
        
        private void LogError(Exception exception, string methodName, Type type, string key)
        {
            _logger.LogError(exception,
                $"Exception occured in the inner {methodName} for {nameof(key)}: {key} and {nameof(type)}: {type}");
        }
    }
}
