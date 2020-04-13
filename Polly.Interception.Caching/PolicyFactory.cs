using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Polly.Caching;

namespace Polly.Interception.Caching
{
    public class PolicyFactory : Interception.IReadOnlyPolicyRegistry
    {
        private readonly IDictionary<string, (IAsyncPolicy asyncPolicy, ISyncPolicy syncPolicy)> _policyDictionary =
            new Dictionary<string, (IAsyncPolicy asyncPolicy, ISyncPolicy syncPolicy)>();
        
        
        bool IReadOnlyPolicyRegistry.TryGetAsyncPolicy(IInvocation invocation, out IAsyncPolicy policy)
        {
            var key = GetKeyOrNull(invocation.MethodInvocationTarget, out _);
            if (key == null)
            {
                policy = null;
                return false;
            }

            policy = _policyDictionary[key].asyncPolicy;

            return true;
        }

        bool IReadOnlyPolicyRegistry.TryGetSyncPolicy(IInvocation invocation, out ISyncPolicy policy)
        {
            var key = GetKeyOrNull(invocation.MethodInvocationTarget,  out _);
            if (key == null)
            {
                policy = null;
                return false;
            }

            policy = _policyDictionary[key].syncPolicy;

            return true;
        }
        
        
        internal bool TryGetOrCreatePolicies(MemberInfo methodInfo, 
            IServiceProvider serviceProvider, 
            out (IAsyncPolicy asyncPolicy, ISyncPolicy syncPolicy) policies)
        {
            var key = GetKeyOrNull(methodInfo, out var ttlStrategy);
            if (key == null)
            {
                policies = default;
                return false;
            }

            if (_policyDictionary.TryGetValue(key, out policies))
            {
                return true;
            }
            
            IAsyncPolicy asyncPolicy = Policy.CacheAsync(serviceProvider.GetService<IAsyncCacheProvider>(), ttlStrategy);
            ISyncPolicy syncPolicy = Policy.Cache(serviceProvider.GetService<ISyncCacheProvider>(), ttlStrategy);

            policies = (asyncPolicy, syncPolicy);
            _policyDictionary[key] = policies;
            
            return true;
        }

        private static string GetKeyOrNull(MemberInfo memberInfo, out ITtlStrategy ttlStrategy)
        {
            var cacheAttribute = memberInfo.GetCustomAttributes<CachePolicyAttribute>().FirstOrDefault();
            if (cacheAttribute == null)
            {
                ttlStrategy = null;
                return null;
            }

            switch (cacheAttribute.ExpirationType)
            {
                case ExpirationType.NoExpiration:
                    ttlStrategy = new RelativeTtl(TimeSpan.MaxValue);
                    break;
                case ExpirationType.Sliding:
                    ttlStrategy = new SlidingTtl(cacheAttribute.Ttl);
                    break;
                case ExpirationType.Relative:
                    ttlStrategy = new RelativeTtl(cacheAttribute.Ttl);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return $"CachePolicy_{cacheAttribute.ExpirationType.ToString()}_{cacheAttribute.Ttl.TotalSeconds}";
        }
    }


    public enum ExpirationType
    {
        Unknown = 0,
        NoExpiration,
        Relative,
        Sliding
    }
}