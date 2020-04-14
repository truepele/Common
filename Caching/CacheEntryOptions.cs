using System;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace Caching
{
    public class CacheEntryOptions
    {
        private readonly TimeSpan _ttl;
        

        public CacheEntryOptions(TimeSpan ttl, ExpirationType type = ExpirationType.Relative)
        {
            Type = type;
            _ttl = ttl;
        }
        
        
        public ExpirationType Type { get; }
        

        public DistributedCacheEntryOptions ToDistributedCacheEntryOptions()
        {
            var distributedCacheOptions = new DistributedCacheEntryOptions();
            switch (Type)
            {
                case ExpirationType.NoExpiration:
                    distributedCacheOptions.SetAbsoluteExpiration(TimeSpan.MinValue);
                    break;
                case ExpirationType.Relative:
                    distributedCacheOptions.SetAbsoluteExpiration(_ttl);
                    break;
                case ExpirationType.Sliding:
                    distributedCacheOptions.SetSlidingExpiration(_ttl);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return distributedCacheOptions;
        }
        
        public MemoryCacheEntryOptions ToMemoryCacheEntryOptions()
        {
            var memoryCacheOptions = new MemoryCacheEntryOptions();
            switch (Type)
            {
                case ExpirationType.NoExpiration:
                    memoryCacheOptions.SetAbsoluteExpiration(TimeSpan.MinValue);
                    break;
                case ExpirationType.Relative:
                    memoryCacheOptions.SetAbsoluteExpiration(_ttl);
                    break;
                case ExpirationType.Sliding:
                    memoryCacheOptions.SetSlidingExpiration(_ttl);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return memoryCacheOptions;
        }
    }
}