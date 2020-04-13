using System;

namespace Polly.Interception.Caching
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CachePolicyAttribute : Attribute
    {
        public CachePolicyAttribute()
        {
            ExpirationType = ExpirationType.NoExpiration;
        }

        public CachePolicyAttribute(int ttlSeconds, ExpirationType expirationType = ExpirationType.Relative)
        {
            Ttl = TimeSpan.FromSeconds(ttlSeconds);
            ExpirationType = expirationType;
        }

        public TimeSpan Ttl { get; }
        public ExpirationType ExpirationType { get; }
    }
}