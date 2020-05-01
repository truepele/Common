using System;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;

namespace Caching.Interception
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection InterceptWithMemoryCacheByAttribute(this IServiceCollection services)
        {
            return services
                .AddSingleton<ICache, MemoryCache>()
                .InterceptWithCacheByAttribute(_ => { });
        }
        
        public static IServiceCollection InterceptWithMemoryCacheByAttribute(this IServiceCollection services, 
            Action<CacheInterceptorOptions> configureInterceptorOptions)
        {
            return services
                .AddSingleton<ICache, MemoryCache>()
                .InterceptWithCacheByAttribute(configureInterceptorOptions);
        }
        
        public static IServiceCollection InterceptWithMemoryCacheByAttribute(this IServiceCollection services, 
            Microsoft.Extensions.Configuration.IConfiguration config)
        {
            return services
                .AddSingleton<ICache, MemoryCache>()
                .InterceptWithCacheByAttribute(config);
        }
        
        public static IServiceCollection InterceptWithDistributedCacheByAttribute(this IServiceCollection services)
        {
            return services
                .AddSingleton<ICache, DistributedCache>()
                .InterceptWithCacheByAttribute();
        }

        public static IServiceCollection InterceptWithDistributedCacheByAttribute(this IServiceCollection services,
            Action<CacheInterceptorOptions> configureInterceptorOptions)
        {
            return services
                .AddSingleton<ICache, DistributedCache>()
                .InterceptWithCacheByAttribute(configureInterceptorOptions);
        }

        public static IServiceCollection InterceptWithDistributedCacheByAttribute(this IServiceCollection services,
            Microsoft.Extensions.Configuration.IConfiguration config)
        {
            return services
                .AddSingleton<ICache, DistributedCache>()
                .InterceptWithCacheByAttribute(config);
        }

        internal static IServiceCollection InterceptWithCacheByAttribute(this IServiceCollection services)
        {
            return services
                .AddSingleton<IProxyGenerator, ProxyGenerator>()
                .AddSingleton<CacheInterceptor>()
                .DecorateWithCacheByAttribute();
        }

        internal static IServiceCollection InterceptWithCacheByAttribute(this IServiceCollection services, 
            Action<CacheInterceptorOptions> configureOptions)
        {
            return services
                .Configure(configureOptions)
                .InterceptWithCacheByAttribute();
        }
        
        internal static IServiceCollection InterceptWithCacheByAttribute(this IServiceCollection services,
            Microsoft.Extensions.Configuration.IConfiguration config)
        {
            return services
                .Configure<CacheInterceptorOptions>(config)
                .InterceptWithCacheByAttribute();
        }

        private static IServiceCollection DecorateWithCacheByAttribute(this IServiceCollection services)
        {
            var servicesToDecorate = services.Where(d =>
                {
                    // We do not support open generics
                    if (d.ServiceType.IsGenericTypeDefinition)
                    {
                        return false;
                    }

                    // TODO: We do not support factory registrations
                    var implementationType = d.ImplementationType ?? d.ImplementationInstance.GetType();
                    if (implementationType == null)
                    {
                        return false;
                    }

                    return implementationType
                        .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                        .Any(m => m.GetCustomAttribute<CacheAttribute>() != null);
                })
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
