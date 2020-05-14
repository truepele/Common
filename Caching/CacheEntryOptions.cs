using System;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace Caching
{
    public class CacheEntryOptions
    {
        public CacheEntryOptions(TimeSpan ttl, ExpirationType type = ExpirationType.Relative)
        {
            Type = type;
            Ttl = ttl;
        }

        public TimeSpan Ttl { get; }
        public ExpirationType Type { get; }
    }
}
