using System;

namespace Caching
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CacheAttribute : Attribute
    {
        public CacheAttribute()
        {
            CacheEntryOptions = new CacheEntryOptions(TimeSpan.MaxValue, ExpirationType.NoExpiration);
        }
        

        public CacheAttribute(double ttlMs, ExpirationType expirationType = ExpirationType.Relative)
        {
            var ttl = TimeSpan.FromMilliseconds(ttlMs);
            CacheEntryOptions = new CacheEntryOptions(ttl, expirationType);
        }


        public CacheEntryOptions CacheEntryOptions { get; }
    }
}