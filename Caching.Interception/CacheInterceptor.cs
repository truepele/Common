using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Caching.Interception
{
    public class CacheInterceptor : IAsyncInterceptor
    {
        private readonly ICache _cache;

        public CacheInterceptor(ICache cache)
        {
            _cache = cache;
        }
        

        public void InterceptSynchronous(IInvocation invocation)
        {
            if (invocation.MethodInvocationTarget.ReturnType == typeof(void)
                || !TryGetCacheEntryOptions(invocation, out var options))
            {
                invocation.Proceed();
                return;
            }

            invocation.ReturnValue = _cache.GetOrCreate(CreateOperationKey(invocation),
                invocation.MethodInvocationTarget.ReturnType,
                options,
                _ =>
                {
                    invocation.Proceed();
                    return invocation.ReturnValue;
                });
        }

        public void InterceptAsynchronous(IInvocation invocation)
        {
            invocation.Proceed();
        }

        public void InterceptAsynchronous<TResult>(IInvocation invocation)
        {
            if(!TryGetCacheEntryOptions(invocation, out var options))
            {
                invocation.Proceed();
                return;
            }

            var capture = invocation.CaptureProceedInfo();
            
            invocation.ReturnValue = _cache.GetOrCreateAsync(CreateOperationKey(invocation),
                options,
                _ =>
                {
                    capture.Invoke();
                    return (Task<TResult>)invocation.ReturnValue;
                });
        }
        
        private static bool TryGetCacheEntryOptions(IInvocation invocation, out CacheEntryOptions cacheEntryOptions)
        {
            cacheEntryOptions = null;

            var cacheAttribute = invocation.MethodInvocationTarget.GetCustomAttribute<CacheAttribute>();
            if (cacheAttribute == null)
            {
                return false;
            }

            cacheEntryOptions = cacheAttribute.CacheEntryOptions;

            return true;
        }

        private static string CreateOperationKey(IInvocation invocation)
        {
            var type = invocation.TargetType;
            var methodInfo = invocation.MethodInvocationTarget;
            var fullMethodName = $"{type.Namespace}.{type.Name}.{methodInfo.Name}";
            var arguments = string.Join("_", invocation.Arguments.Select(a => a.ToString()));
            var operationKey = $"{methodInfo.ReturnType} | {fullMethodName} | {arguments}";
            return operationKey;
        }
    }
}