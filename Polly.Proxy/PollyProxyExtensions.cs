using Castle.DynamicProxy;

namespace Polly.Proxy
{
    public static class PollyProxyExtensions
    {
        private static readonly ProxyGenerator Generator = new ProxyGenerator();
        
        public static T WithPolicy<T>(this T instance, AsyncPolicy policy) where T : class
        {
            var interceptor = new AsyncPolicyInterceptor(policy);

            return typeof(T).IsInterface 
                ? Generator.CreateInterfaceProxyWithTargetInterface(instance, interceptor) 
                : Generator.CreateClassProxyWithTarget(instance, interceptor);
        }
    }
}
