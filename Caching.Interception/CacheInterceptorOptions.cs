using System;

namespace Caching.Interception
{
    public class CacheInterceptorOptions
    {
        public TimeSpan DefaultTtl { get; set; }
        public ExpirationType DefaultExpirationType { get; set; } = ExpirationType.NoExpiration;
    }
}
