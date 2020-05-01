using System;

namespace Caching.Interception
{
    public class CacheInterceptorOptions
    {
        public TimeSpan DefaultTtl { get; set; } = TimeSpan.MaxValue;
        public ExpirationType DefaultExpirationType { get; set; } = ExpirationType.Relative;
    }
}
