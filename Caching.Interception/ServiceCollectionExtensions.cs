using System;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;

namespace Caching.Interception
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection InterceptWithMemoryCacheByAttribute(this IServiceCollection services)
        {
            return services
                .AddMemoryCache()
                .AddSingleton<ICache, MemoryCache>()
                .InterceptWithCacheByAttribute();
        }
        
        public static IServiceCollection InterceptWithStackExchangeRedisCacheByAttribute(this IServiceCollection services,
            Action<RedisCacheOptions> setupAction)
        {
            return services.AddStackExchangeRedisCache(setupAction)
                .AddSingleton<ICache, DistributedCache>()
                .InterceptWithCacheByAttribute();
        }
        
        internal static IServiceCollection InterceptWithCacheByAttribute(this IServiceCollection services)
        { 
            services.AddMemoryCache()
                .AddSingleton<IProxyGenerator, ProxyGenerator>()
                .AddSingleton<CacheInterceptor>();
            
            var servicesToDecorate = services.Where(d =>
                    !d.ServiceType.IsGenericTypeDefinition // We do not support open generics
                    && d.ImplementationType != null // We do not support factory instances
                    && d.ImplementationType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                        .Any(m => m.GetCustomAttribute<CacheAttribute>() != null))
                .ToArray();

            foreach (var d in servicesToDecorate)
            {
                services.Decorate(d.ServiceType, (instance, provider) =>
                {
                    var generator = provider.GetRequiredService<IProxyGenerator>();
                    var interceptor = provider.GetRequiredService<CacheInterceptor>();
                    return d.ServiceType.IsInterface 
                        ? generator.CreateInterfaceProxyWithTargetInterface(d.ServiceType, instance, interceptor) 
                        : generator.CreateClassProxyWithTarget(d.ServiceType, instance, interceptor);
                });
            }

            return services;
        }
    }
}
