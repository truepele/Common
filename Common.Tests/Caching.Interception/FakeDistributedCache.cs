using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace Common.Tests.Caching.Interception
{
    public class FakeDistributedCache : IDistributedCache
    {
        private readonly IMemoryCache _memoryCache;

        public FakeDistributedCache(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }
        
        
        public byte[] Get(string key)
        {
            return _memoryCache.Get<byte[]>(key);
        }

        public Task<byte[]> GetAsync(string key, CancellationToken token = new CancellationToken())
        {
            return Task.FromResult(_memoryCache.Get<byte[]>(key));
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            _memoryCache.Set(key,
                value,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = options.AbsoluteExpiration,
                    SlidingExpiration = options.SlidingExpiration,
                    AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow
                });
        }

        public Task SetAsync(string key, byte[] value, 
            DistributedCacheEntryOptions options, 
            CancellationToken token = new CancellationToken())
        {
            _memoryCache.Set(key,
                value,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = options.AbsoluteExpiration,
                    SlidingExpiration = options.SlidingExpiration,
                    AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow
                });
            
            return Task.CompletedTask;
        }

        public void Refresh(string key)
        {
            throw new NotSupportedException();
        }

        public Task RefreshAsync(string key, CancellationToken token = new CancellationToken())
        {
            throw new NotSupportedException();
        }

        public void Remove(string key)
        {
            throw new NotSupportedException();
        }

        public Task RemoveAsync(string key, CancellationToken token = new CancellationToken())
        {
            throw new NotSupportedException();
        }
    }
}
