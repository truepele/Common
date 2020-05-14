using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caching;
using Caching.Interception;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Common.Tests.Caching
{
    [TestFixture]
    public class CacheInterceptorTests
    {
        public interface IService
        {
            Task DoStuffAsync();
            void DoStuff();
            
            Task<string> GetStuffNoAttrAsync();
            
            string GetStuffNoAttr();
            
            Task<string> GetStuffAsync(CancellationToken cancellationToken);
            
            string GetStuff();
        }
        
        public class Service : IService
        {
            private readonly string _val;
            public IDictionary<string, int> ReceivedCalls { get; } = new Dictionary<string, int>();

            public Service(string val)
            {
                if (string.IsNullOrEmpty(val))
                {
                    throw new ArgumentException("Value cannot be null or empty.", nameof(val));
                }

                _val = val;
            }
            
            public Task DoStuffAsync()
            {
                AddCall(nameof(DoStuffAsync));
                return Task.Delay(1);
            }

            public void DoStuff()
            {
                AddCall(nameof(DoStuff));
            }

            public async Task<string> GetStuffNoAttrAsync()
            {
                AddCall(nameof(GetStuffNoAttrAsync));
                await Task.Delay(1);
                return _val;
            }

            public string GetStuffNoAttr()
            {
                AddCall(nameof(GetStuffNoAttr));
                return _val;
            }

            [Cache]
            public async Task<string> GetStuffAsync(CancellationToken cancellationToken)
            {
                AddCall(nameof(GetStuffAsync));
                await Task.Delay(1);
                return _val;
            }

            [Cache]
            public string GetStuff()
            {
                AddCall(nameof(GetStuff));
                return _val;
            }

            private void AddCall(string methodName)
            {
                if (ReceivedCalls.TryGetValue(methodName, out var prevValue))
                {
                    ReceivedCalls[methodName] = prevValue + 1;
                }
                else
                {
                    ReceivedCalls[methodName] = 1;
                }
            }
        }

        private static IServiceProvider BuildServiceProvider(ICache cache, Service target, 
            Action<CacheInterceptorOptions> configureOptions = null)
        {
            var services = new ServiceCollection()
                .AddSingleton(cache)
                .AddSingleton<IService>(target);

            if (configureOptions != null)
            {
                services.InterceptWithCacheByAttribute(configureOptions);
            }
            else
            {
                services.InterceptWithCacheByAttribute();
            }

            return services.BuildServiceProvider();
        }

        [Test]
        public async Task AsyncVoidMethod_InterceptorCallsTarget()
        {
            // Arrange
            var target = new Service(Guid.NewGuid().ToString());
            var cache = Substitute.For<ICache>();
            var serviceProvider = BuildServiceProvider(cache, target);
            var instance = serviceProvider.GetRequiredService<IService>();
            
            // Act
            await instance.DoStuffAsync();
            
            // Assert
            Assert.AreEqual(1, target.ReceivedCalls[nameof(target.DoStuffAsync)]);
            Assert.IsEmpty(cache.ReceivedCalls());
        }
        
        
        [Test]
        public async Task AsyncMethodNoAttribute_InterceptorCallsTarget()
        {
            // Arrange
            
            var val = Guid.NewGuid().ToString();
            var cache = Substitute.For<ICache>();
            var target = new Service(val);
            var serviceProvider = BuildServiceProvider(cache, target);
            var instance = serviceProvider.GetRequiredService<IService>();
            
            // Act
            var result = await instance.GetStuffNoAttrAsync();
            
            // Assert
            Assert.IsEmpty(cache.ReceivedCalls());
            Assert.AreEqual(val, result);
        }
        
        
        [Test]
        public void SyncVoidMethod_InterceptorCallsTarget()
        {
            // Arrange
            var cache = Substitute.For<ICache>();
            var target = new Service(Guid.NewGuid().ToString());
            var serviceProvider = BuildServiceProvider(cache, target);
            var instance = serviceProvider.GetRequiredService<IService>();
            
            // Act
            instance.DoStuff();
            
            // Assert
            Assert.AreEqual(1, target.ReceivedCalls[nameof(target.DoStuff)]);
            Assert.IsEmpty(cache.ReceivedCalls());
        }
        
        [Test]
        public void SyncMethodNoAttribute_InterceptorCallsTarget()
        {
            // Arrange
            var cache = Substitute.For<ICache>();
            var val = Guid.NewGuid().ToString();
            var target = new Service(val);
            var serviceProvider = BuildServiceProvider(cache, target);
            var instance = serviceProvider.GetRequiredService<IService>();
            
            // Act
            var result = instance.GetStuffNoAttr();
            
            // Assert
            Assert.IsEmpty(cache.ReceivedCalls());
            Assert.AreEqual(val, result);
        }
        
        [Test]
        public void SyncMethod_Intercepted()
        {
            // Arrange
            
            var cacheValue = Guid.NewGuid().ToString();
            var cache = Substitute.For<ICache>();
            cache.GetOrCreate(Arg.Any<string>(),
                    typeof(string), 
                    Arg.Any<CacheEntryOptions>(),
                    Arg.Any<Func<string, object>>()
                    )
                .Returns(cacheValue);

            var target = new Service(Guid.NewGuid().ToString());
                
            var serviceProvider = BuildServiceProvider(cache, target);
            var instance = serviceProvider.GetRequiredService<IService>();
           
            
            // Act
            var result1 = instance.GetStuff();
            var result2 = instance.GetStuff();
            
            // Assert
            Assert.AreEqual(2, cache.ReceivedCalls().Count());
            Assert.AreEqual(result1, result2);
            Assert.AreEqual(cacheValue, result1);
        }
        
        [Test]
        public async Task AsyncMethod_Intercepted()
        {
            // Arrange
            
            var cacheValue = Guid.NewGuid().ToString();
            var cache = Substitute.For<ICache>();
            cache.GetOrCreateAsync(Arg.Any<string>(),
                    Arg.Any<CacheEntryOptions>(),
                    Arg.Any<Func<string, Task<string>>>()
                )
                .Returns(cacheValue);

            var target = new Service(Guid.NewGuid().ToString());
                
            var serviceProvider = BuildServiceProvider(cache, target);
            var instance = serviceProvider.GetRequiredService<IService>();
            
            
            // Act
            var result1 = await instance.GetStuffAsync(CancellationToken.None);
            var result2 = await instance.GetStuffAsync(CancellationToken.None);
            
            // Assert
            Assert.AreEqual(2, cache.ReceivedCalls().Count());
            Assert.AreEqual(result1, result2);
            Assert.AreEqual(cacheValue, result1);
        }
        
        
        [TestCase(ExpirationType.Relative, 1)]
        [TestCase(ExpirationType.Relative, 2)]
        [TestCase(ExpirationType.Sliding, 3)]
        [TestCase(ExpirationType.Sliding, 4)]
        public async Task CacheInterceptorOptionsConfigured(ExpirationType expirationType, int seconds)
        {
            // Arrange
            var cacheValue = Guid.NewGuid().ToString();
            var cache = Substitute.For<ICache>();
            cache.GetOrCreateAsync(Arg.Any<string>(),
                    Arg.Any<CacheEntryOptions>(),
                    Arg.Any<Func<string, Task<string>>>()
                )
                .Returns(cacheValue);

            var target = new Service(Guid.NewGuid().ToString());
                
            var serviceProvider = BuildServiceProvider(cache, target,
                o =>
                {
                    o.DefaultTtl = TimeSpan.FromSeconds(seconds);
                    o.DefaultExpirationType = expirationType;
                });
            var instance = serviceProvider.GetRequiredService<IService>();
            
            
            // Act
            var result = await instance.GetStuffAsync(CancellationToken.None);
            
            // Assert
            await cache.Received(1).GetOrCreateAsync(Arg.Any<string>(),
                Arg.Is<CacheEntryOptions>(o =>
                    o.Type == expirationType
                    && o.Ttl.TotalSeconds == seconds),
                Arg.Any<Func<string, Task<string>>>(),
                Arg.Any<CancellationToken>());
        }
    }
}
