using System;
using Castle.DynamicProxy;
using Polly.Registry;

namespace Polly.Interception
{
    public static class ProxyGenerationExtensions
    {
        public static T InterceptWithPolicy<T>(this IProxyGenerator generator, 
            T instance, 
            IReadOnlyPolicyRegistry policyReadOnlyRegistry) 
            where T : class
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (policyReadOnlyRegistry == null) throw new ArgumentNullException(nameof(policyReadOnlyRegistry));

            var interceptor = new PolicyInterceptor(policyReadOnlyRegistry);
            return generator.Intercept(instance, interceptor);
        }
        
        public static object InterceptWithPolicy(this IProxyGenerator generator, 
            Type type,
            object instance, 
            IReadOnlyPolicyRegistry policyReadOnlyRegistry)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (policyReadOnlyRegistry == null) throw new ArgumentNullException(nameof(policyReadOnlyRegistry));

            var interceptor = new PolicyInterceptor(policyReadOnlyRegistry);
            return generator.Intercept(type, instance, interceptor);
        }
        

        private static T Intercept<T>(this IProxyGenerator generator, T instance, IAsyncInterceptor interceptor)
        where T : class
        {
            return typeof(T).IsInterface 
                ? generator.CreateInterfaceProxyWithTargetInterface(instance, interceptor) 
                : generator.CreateClassProxyWithTarget(instance, interceptor);
        }
        
        private static object Intercept(this IProxyGenerator generator, Type type, object instance, IAsyncInterceptor interceptor)
        {
            return type.IsInterface 
                ? generator.CreateInterfaceProxyWithTargetInterface(type, instance, interceptor) 
                : generator.CreateClassProxyWithTarget(type, instance, interceptor);
        }
    }
}
