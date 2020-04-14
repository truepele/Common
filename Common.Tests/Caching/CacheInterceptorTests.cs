using System;
using System.Linq;
using System.Threading.Tasks;
using Caching;
using Caching.Interception;
using Castle.DynamicProxy;
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
            
            Task<string> GetStuffAsync();
            
            string GetStuff();
        }
        
        public class Service : IService
        {
            private readonly string _val;

            public Service(string val)
            {
                if (string.IsNullOrEmpty(val)) throw new ArgumentException("Value cannot be null or empty.", nameof(val));
                _val = val;
            }
            
            public Task DoStuffAsync()
            {
                throw new NotSupportedException();
            }

            public void DoStuff()
            {
                throw new NotSupportedException();
            }

            public Task<string> GetStuffNoAttrAsync()
            {
                return Task.FromResult(_val);
            }

            public string GetStuffNoAttr()
            {
                return _val;
            }

            [Cache]
            public Task<string> GetStuffAsync()
            {
                return Task.FromResult(_val);
            }

            [Cache]
            public string GetStuff()
            {
                return _val;
            }
        }

        [Test]
        public async Task AsyncVoidMethod_InterceptorCallsTarget()
        {
            // Arrange
            var cache = Substitute.For<ICache>();
            var target = Substitute.For<IService>();
            var interceptor = new CacheInterceptor(cache);
            var generator = new ProxyGenerator();
            var instance = generator.CreateInterfaceProxyWithTargetInterface(target, interceptor);
            
            // Act
            await instance.DoStuffAsync();
            
            // Assert
            await target.Received(1).DoStuffAsync();
            Assert.IsEmpty(cache.ReceivedCalls());
        }
        
        
        [Test]
        public async Task AsyncMethodNoAttribute_InterceptorCallsTarget()
        {
            // Arrange
            var cache = Substitute.For<ICache>();

            var val = Guid.NewGuid().ToString();
            var target = Substitute.For<IService>();
            target.GetStuffNoAttrAsync().Returns(val);
                
            var interceptor = new CacheInterceptor(cache);
            var generator = new ProxyGenerator();
            var instance = generator.CreateInterfaceProxyWithTargetInterface(target, interceptor);
            
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
            var target = Substitute.For<IService>();
            var interceptor = new CacheInterceptor(cache);
            var generator = new ProxyGenerator();
            var instance = generator.CreateInterfaceProxyWithTargetInterface(target, interceptor);
            
            // Act
            instance.DoStuff();
            
            // Assert
            target.Received(1).DoStuff();
            Assert.IsEmpty(cache.ReceivedCalls());
        }
        
        [Test]
        public void SyncMethodNoAttribute_InterceptorCallsTarget()
        {
            // Arrange
            var cache = Substitute.For<ICache>();

            var val = Guid.NewGuid().ToString();
            var target = Substitute.For<IService>();
            target.GetStuffNoAttr().Returns(val);
                
            var interceptor = new CacheInterceptor(cache);
            var generator = new ProxyGenerator();
            var instance = generator.CreateInterfaceProxyWithTargetInterface(target, interceptor);
            
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

            IService target = new Service(Guid.NewGuid().ToString());
                
            var interceptor = new CacheInterceptor(cache);
            var generator = new ProxyGenerator();
            var instance = generator.CreateInterfaceProxyWithTargetInterface(target, interceptor);
            
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

            IService target = new Service(Guid.NewGuid().ToString());
                
            var interceptor = new CacheInterceptor(cache);
            var generator = new ProxyGenerator();
            var instance = generator.CreateInterfaceProxyWithTargetInterface(target, interceptor);
            
            // Act
            var result1 = await instance.GetStuffAsync();
            var result2 = await instance.GetStuffAsync();
            
            // Assert
            Assert.AreEqual(2, cache.ReceivedCalls().Count());
            Assert.AreEqual(result1, result2);
            Assert.AreEqual(cacheValue, result1);
        }
    }
}
