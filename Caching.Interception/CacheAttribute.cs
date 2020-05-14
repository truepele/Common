using System;

namespace Caching.Interception
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CacheAttribute : Attribute
    {
        public ExpirationType? ExpirationType { get; }
        public TimeSpan Ttl { get; }
        
        
        public CacheAttribute()
        {
        }
        
        public CacheAttribute(double ttlMs, ExpirationType expirationType = Caching.ExpirationType.Relative)
        {
            Ttl = TimeSpan.FromMilliseconds(ttlMs);
            ExpirationType = expirationType;
        }
    }
}
