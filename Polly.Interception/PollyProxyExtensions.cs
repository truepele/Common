using System;
using Castle.DynamicProxy;

namespace Polly.Interception
{
    public static class PollyProxyExtensions
    {
        private static readonly ProxyGenerator Generator = new ProxyGenerator();
        
        public static T WithPolicy<T>(this T instance, AsyncPolicy policy) where T : class
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            
            var interceptor = new AsyncPolicyInterceptor(policy);

            return typeof(T).IsInterface 
                ? Generator.CreateInterfaceProxyWithTargetInterface(instance, interceptor) 
                : Generator.CreateClassProxyWithTarget(instance, interceptor);
        }
    }
}
