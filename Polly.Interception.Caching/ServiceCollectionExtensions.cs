using System;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;

namespace Polly.Interception.Caching
{
    public static class ServiceCollectionExtensions
    {
        private static readonly PolicyFactory PolicyFactory = new PolicyFactory();
        private static readonly ProxyGenerator Generator = new ProxyGenerator();
        
        public static IServiceCollection AddCachePolicyInterception(this IServiceCollection services) 
        {
            foreach (var d in services.ToArray())
            {
                // We do not support open generics
                if (d.ServiceType.IsGenericTypeDefinition)
                {
                    continue;
                }

                services.Decorate(d.ServiceType, (instance, provider) =>
                {
                    if (d.ImplementationType == null)
                    {
                        return instance;
                    }

                    var policies = d.ImplementationType
                        .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                        .Select(m => PolicyFactory.TryGetOrCreatePolicies(m, provider, out _))
                        .ToList(); // Ensure enumerated so that all policies created according to attributes on the type (if any)

                    if (policies.All(policyCreated => !policyCreated))
                    {
                        return instance;
                    }

                    return Generator.InterceptWithPolicy(d.ServiceType, instance, PolicyFactory);
                });
            }

            return services;
        }
    }
}