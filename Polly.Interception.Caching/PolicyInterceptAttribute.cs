using System;

namespace Polly.Interception.Caching
{
    public abstract class _PolicyInterceptAttributeBase : Attribute
    {
        public _PolicyInterceptAttributeBase(string registryKeyPrefix)
        {
            if (string.IsNullOrWhiteSpace(registryKeyPrefix))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(registryKeyPrefix));
            
            RegistryKeyPrefix = registryKeyPrefix;
        }
        
        public string RegistryKeyPrefix { get; }
    }
}