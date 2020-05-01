using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.Extensions.Options;

namespace Caching.Interception
{
    public class CacheInterceptor : IAsyncInterceptor
    {
        private readonly ICache _cache;
        private readonly CacheInterceptorOptions _options;

        public CacheInterceptor(ICache cache, IOptions<CacheInterceptorOptions> options = null)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _options = options?.Value ?? new CacheInterceptorOptions();
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
            
            var cancellationArg = invocation.Arguments.FirstOrDefault(a => a is CancellationToken);

            invocation.ReturnValue = _cache.GetOrCreateAsync(CreateOperationKey(invocation),
                options,
                _ =>
                {
                    capture.Invoke();
                    return (Task<TResult>)invocation.ReturnValue;
                },
                (CancellationToken?)cancellationArg ?? default);
        }
        
        private bool TryGetCacheEntryOptions(IInvocation invocation, out CacheEntryOptions cacheEntryOptions)
        {
            cacheEntryOptions = null;

            var cacheAttribute = invocation.MethodInvocationTarget.GetCustomAttribute<CacheAttribute>();
            if (cacheAttribute == null)
            {
                return false;
            }

            cacheEntryOptions = cacheAttribute.ExpirationType == null 
                ? new CacheEntryOptions(_options.DefaultTtl, _options.DefaultExpirationType) 
                : new CacheEntryOptions(cacheAttribute.Ttl, cacheAttribute.ExpirationType.Value);

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
