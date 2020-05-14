using System;
using System.Threading.Tasks;
using Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Common.Tests.Caching
{
    [TestFixture]
    public class ExceptionHandlingCacheDecoratorTests
    {
        [Test]
        public void Ctor_DoesNullChecks()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ExceptionHandlingCacheDecorator(null, Substitute.For<ILogger<ExceptionHandlingCacheDecorator>>()));
            Assert.Throws<ArgumentNullException>(() =>
                new ExceptionHandlingCacheDecorator(Substitute.For<ICache>(), null));
            Assert.Throws<ArgumentNullException>(() =>
                new ExceptionHandlingCacheDecorator(null, null));
        }

        [Test]
        public void Get_HandlesException()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var cache = Substitute.For<ICache>();
            cache.Get(key, typeof(AClass)).Throws(new Exception());
            var logger = Substitute.For<ILogger<ExceptionHandlingCacheDecorator>>();
            var decorator = new ExceptionHandlingCacheDecorator(cache, logger);
            
            // Act
            var result = decorator.Get(key, typeof(AClass));
            
            // Assert
            Assert.IsNull(result);
        }
        
        [Test]
        public void GetIfCached_HandlesException()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var cache = Substitute.For<ICache>();
            cache.GetIfCached(key, out Arg.Any<AClass>()).Throws(new Exception());
            var logger = Substitute.For<ILogger<ExceptionHandlingCacheDecorator>>();
            var decorator = new ExceptionHandlingCacheDecorator(cache, logger);
            
            // Act
            var result = decorator.GetIfCached<AClass>(key, out var value);
            
            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(value);
        }
        
        [Test]
        public void GetIfCached_ReturnsCachedValue()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var cachedValue = new AClass();
            var cache = Substitute.For<ICache>();
            cache.GetIfCached(key, out Arg.Any<AClass>()).Returns(x =>
            {
                x[1] = cachedValue;
                return true;
            });
            var logger = Substitute.For<ILogger<ExceptionHandlingCacheDecorator>>();
            var decorator = new ExceptionHandlingCacheDecorator(cache, logger);
            
            // Act
            var result = decorator.GetIfCached<AClass>(key, out var value);
            
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(cachedValue, value);
        }

        [Test]
        public async Task GetIfCachedAsync_HandlesException()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var cache = Substitute.For<ICache>();
            cache.GetIfCachedAsync<AClass>(key).Throws(new Exception());
            var logger = Substitute.For<ILogger<ExceptionHandlingCacheDecorator>>();
            var decorator = new ExceptionHandlingCacheDecorator(cache, logger);
            
            // Act
            (bool wasInCache, AClass value) = await decorator.GetIfCachedAsync<AClass>(key);
            
            // Assert
            Assert.IsFalse(wasInCache);
            Assert.IsNull(value);
        }
        
        [Test]
        public void GetIfCachedAsync_ReturnsCachedValue()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var cachedValue = new AClass();
            var cache = Substitute.For<ICache>();
            cache.GetIfCached(key, out Arg.Any<AClass>()).Returns(x =>
            {
                x[1] = cachedValue;
                return true;
            });
            var logger = Substitute.For<ILogger<ExceptionHandlingCacheDecorator>>();
            var decorator = new ExceptionHandlingCacheDecorator(cache, logger);
            
            // Act
            var result = decorator.GetIfCached<AClass>(key, out var value);
            
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(cachedValue, value);
        }
        
        
        
        
        
        
        [Test]
        public async Task GetAsync_HandlesException()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var cache = Substitute.For<ICache>();
            cache.GetAsync<AClass>(key).Throws(new Exception());
            var logger = Substitute.For<ILogger<ExceptionHandlingCacheDecorator>>();
            var decorator = new ExceptionHandlingCacheDecorator(cache, logger);
            
            // Act
            var result = await decorator.GetAsync<AClass>(key);
            
            // Assert
            Assert.IsNull(result);
        }
        
        [Test]
        public void GetGeneric_HandlesException()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var cache = Substitute.For<ICache>();
            cache.Get<AClass>(key).Throws(new Exception());
            var logger = Substitute.For<ILogger<ExceptionHandlingCacheDecorator>>();
            var decorator = new ExceptionHandlingCacheDecorator(cache, logger);
            
            // Act
            var result = decorator.Get<AClass>(key);
            
            // Assert
            Assert.IsNull(result);
        }
        
        [Test]
        public void GetOrCreate_HandlesGetException()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var factoryValue = new AClass();
            var cache = Substitute.For<ICache>();
            cache.Get(key, typeof(AClass)).Throws(new Exception());
            var logger = Substitute.For<ILogger<ExceptionHandlingCacheDecorator>>();
            var decorator = new ExceptionHandlingCacheDecorator(cache, logger);
            
            // Act
            var result = decorator.GetOrCreate(key, 
                typeof(AClass), 
                new CacheEntryOptions(new TimeSpan()), 
                _ => factoryValue );
            
            // Assert
            Assert.AreEqual(factoryValue, result);
        }
        
        [Test]
        public void GetOrCreate_HandlesSetException()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var factoryValue = new AClass();
            var options = new CacheEntryOptions(TimeSpan.FromMilliseconds(1));
            var cache = Substitute.For<ICache>();
            cache.When(c => c.Set(key, factoryValue, options)).Do(_ => throw new Exception());
            var logger = Substitute.For<ILogger<ExceptionHandlingCacheDecorator>>();
            var decorator = new ExceptionHandlingCacheDecorator(cache, logger);
            
            // Act
            var result = decorator.GetOrCreate(key, 
                typeof(AClass), 
                options, 
                _ => factoryValue );
            
            // Assert
            Assert.AreEqual(factoryValue, result);
        }
        
        [Test]
        public void GetOrCreateGeneric_HandlesGetException()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var factoryValue = new AClass();
            Func<string, AClass> factory = _ => factoryValue;
            var options = new CacheEntryOptions(TimeSpan.FromMilliseconds(1));
            var cache = Substitute.For<ICache>();
            cache.Get<AClass>(key).Throws(new Exception());
            cache.GetOrCreate(key, options, Arg.Any<Func<string, AClass>>()).Throws(new Exception());
            var logger = Substitute.For<ILogger<ExceptionHandlingCacheDecorator>>();
            var decorator = new ExceptionHandlingCacheDecorator(cache, logger);
            
            // Act
            var result = decorator.GetOrCreate(key, options, factory);
            
            // Assert
            Assert.AreEqual(factoryValue, result);
        }
        
        [Test]
        public void GetOrCreateGeneric_HandlesSetException()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var factoryValue = new AClass();
            Func<string, AClass> factory = _ => factoryValue;
            var options = new CacheEntryOptions(TimeSpan.FromMilliseconds(1));
            var cache = Substitute.For<ICache>();
            cache.When(c => c.Set(key, factoryValue, options)).Do(_ => throw new Exception());
            cache.GetOrCreate(key, options, Arg.Any<Func<string, AClass>>()).Throws(new Exception());
            var logger = Substitute.For<ILogger<ExceptionHandlingCacheDecorator>>();
            var decorator = new ExceptionHandlingCacheDecorator(cache, logger);
            
            // Act
            var result = decorator.GetOrCreate(key,
                options, 
                factory );
            
            // Assert
            Assert.AreEqual(factoryValue, result);
        }
        
        [Test]
        public void GetOrCreateGeneric_ReturnsCachedValue()
        {
            // Arrange
            
            var key = Guid.NewGuid().ToString();
            var cachedValue = new AClass();
            Func<string, AClass> factory = _ => new AClass();
            var options = new CacheEntryOptions(TimeSpan.FromMilliseconds(1));
            
            var cache = Substitute.For<ICache>();
            cache.GetIfCached(key, out Arg.Any<AClass>()).Returns(x =>
            {
                x[1] = cachedValue;
                return true;
            });
            cache.GetOrCreate(key, options, Arg.Any<Func<string, AClass>>()).Returns(cachedValue);
            cache.Get(key, typeof(AClass)).Returns(cachedValue);
            
            var logger = Substitute.For<ILogger<ExceptionHandlingCacheDecorator>>();
            var decorator = new ExceptionHandlingCacheDecorator(cache, logger);
            
            
            // Act
            var result = decorator.GetOrCreate(key, options, factory);
            
            // Assert
            Assert.AreEqual(cachedValue, result);
        }
        
        [Test]
        public void GetOrCreateGeneric_CallsFaultyFactoryOnce()
        {
            // Arrange
            
            var key = Guid.NewGuid().ToString();
            var options = new CacheEntryOptions(TimeSpan.FromMilliseconds(1));
            
            var factoryCallCount = 0;
            Func<string, AClass> factory = _ =>
            {
                factoryCallCount++;
                throw new Exception();
            };
            
            var cache = Substitute.For<ICache>();
            cache.Get<AClass>(key).Returns((AClass)null);
            cache.When(c => c.GetOrCreate(key, options, Arg.Any<Func<string, AClass>>()))
                .Do(c => c.Arg<Func<string, AClass>>()(key));
            
            var logger = Substitute.For<ILogger<ExceptionHandlingCacheDecorator>>();
            var decorator = new ExceptionHandlingCacheDecorator(cache, logger);
            
            
            // Act / Assert
            Assert.Throws<Exception>( () => decorator.GetOrCreate(key, options, factory));
            Assert.AreEqual(1, factoryCallCount);
        }



        [Test]
        public async Task GetOrCreateAsync_HandlesGetException()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var factoryValue = new AClass();
            Func<string, Task<AClass>> factory = _ => Task.FromResult(factoryValue);
            var options = new CacheEntryOptions(TimeSpan.FromMilliseconds(1));
            var cache = Substitute.For<ICache>();
            cache.GetAsync<AClass>(key).Throws(new Exception());
            cache.GetOrCreateAsync(key, options, Arg.Any<Func<string, Task<AClass>>>()).Throws(new Exception());
            var logger = Substitute.For<ILogger<ExceptionHandlingCacheDecorator>>();
            var decorator = new ExceptionHandlingCacheDecorator(cache, logger);
            
            // Act
            var result = await decorator.GetOrCreateAsync(key, options, factory);
            
            // Assert
            Assert.AreEqual(factoryValue, result);
        }
        
        [Test]
        public async Task GetOrCreateAsync_HandlesSetException()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var factoryValue = new AClass();
            Func<string, Task<AClass>> factory = _ => Task.FromResult(factoryValue);
            var options = new CacheEntryOptions(TimeSpan.FromMilliseconds(1));
            var cache = Substitute.For<ICache>();
            cache.GetAsync<AClass>(key).Returns((AClass)null);
            cache.When(c => c.Set(key, factoryValue, options)).Do(_ => throw new Exception());
            cache.GetOrCreateAsync(key, options, Arg.Any<Func<string, Task<AClass>>>()).Throws(new Exception());
            var logger = Substitute.For<ILogger<ExceptionHandlingCacheDecorator>>();
            var decorator = new ExceptionHandlingCacheDecorator(cache, logger);
            
            // Act
            var result = await decorator.GetOrCreateAsync(key,
                options, 
                factory );
            
            // Assert
            Assert.AreEqual(factoryValue, result);
        }
        
        [Test]
        public async Task GetOrCreateAsync_ReturnsCachedValue()
        {
            // Arrange
            
            var key = Guid.NewGuid().ToString();
            var cachedValue = new AClass();
            Func<string, Task<AClass>> factory = _ => Task.FromResult(new AClass());
            var options = new CacheEntryOptions(TimeSpan.FromMilliseconds(1));
            
            var cache = Substitute.For<ICache>();
            cache.GetIfCachedAsync<AClass>(key).Returns((true, cachedValue));
            cache.GetAsync<AClass>(key).Returns(cachedValue);
            cache.GetOrCreateAsync(key, options, Arg.Any<Func<string, Task<AClass>>>()).Returns(cachedValue);
            
            var logger = Substitute.For<ILogger<ExceptionHandlingCacheDecorator>>();
            var decorator = new ExceptionHandlingCacheDecorator(cache, logger);
            
            
            // Act
            var result = await decorator.GetOrCreateAsync(key, options, factory);
            
            // Assert
            Assert.AreEqual(cachedValue, result);
        }
        
        [Test]
        public async Task GetOrCreateAsync_CreatesValueType()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var factoryValue = 1234;
            Func<string, Task<int>> factory = _ => Task.FromResult(factoryValue);
            var options = new CacheEntryOptions(TimeSpan.FromMilliseconds(1));
            var cache = Substitute.For<IDistributedCache>();
            cache.GetAsync(key).Returns((byte[])null);
            var logger = Substitute.For<ILogger<ExceptionHandlingCacheDecorator>>();
            var decorator =
                new ExceptionHandlingCacheDecorator(new DistributedCache(cache, Substitute.For<ILogger<DistributedCache>>()),
                    logger);
            
            // Act
            var result = await decorator.GetOrCreateAsync(key, options, factory);
            
            // Assert
            Assert.AreEqual(factoryValue, result);
        }
        
        [Test]
        public void GetOrCreateGeneric_CreatesValueType()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var factoryValue = 1234;
            Func<string, int> factory = _ => factoryValue;
            var options = new CacheEntryOptions(TimeSpan.FromMilliseconds(1));
            var cache = Substitute.For<IDistributedCache>();
            cache.Get(key).Returns((byte[])null);
            var logger = Substitute.For<ILogger<ExceptionHandlingCacheDecorator>>();
            var decorator =
                new ExceptionHandlingCacheDecorator(new DistributedCache(cache, Substitute.For<ILogger<DistributedCache>>()),
                    logger);
            
            // Act
            var result = decorator.GetOrCreate(key, options, factory);
            
            // Assert
            Assert.AreEqual(factoryValue, result);
        }
        
        [Test]
        public void GetOrCreateAsync_CallsFaultyFactoryOnce()
        {
            // Arrange
            
            var key = Guid.NewGuid().ToString();
            var options = new CacheEntryOptions(TimeSpan.FromMilliseconds(1));
            
            var factoryCallCount = 0;
            Func<string, Task<AClass>> factory = _ =>
            {
                factoryCallCount++;
                throw new Exception();
            };
            
            var cache = Substitute.For<ICache>();
            cache.GetAsync<AClass>(key).Returns((AClass)null);
            cache.When(c => c.GetOrCreateAsync(key, options, Arg.Any<Func<string, Task<AClass>>>()))
                .Do(c => c.Arg<Func<string, AClass>>()(key));
            
            var logger = Substitute.For<ILogger<ExceptionHandlingCacheDecorator>>();
            var decorator = new ExceptionHandlingCacheDecorator(cache, logger);
            
            
            // Act / Assert
            Assert.ThrowsAsync<Exception>( () => decorator.GetOrCreateAsync(key, options, factory));
            Assert.AreEqual(1, factoryCallCount);
        }
        
        
        
        
        
        [Test]
        public void GetOrCreate_CallsFaultyFactoryOnce()
        {
            // Arrange
            
            var key = Guid.NewGuid().ToString();
            var options = new CacheEntryOptions(TimeSpan.FromMilliseconds(1));
            
            var factoryCallCount = 0;
            Func<string, AClass> factory = _ =>
            {
                factoryCallCount++;
                throw new Exception();
            };
            
            var cache = Substitute.For<ICache>();
            cache.Get(key, typeof(AClass)).Returns((AClass)null);
            cache.When(c => c.GetOrCreate(key, typeof(AClass), options, Arg.Any<Func<string, AClass>>()))
                .Do(c => c.Arg<Func<string, AClass>>()(key));
            
            var logger = Substitute.For<ILogger<ExceptionHandlingCacheDecorator>>();
            var decorator = new ExceptionHandlingCacheDecorator(cache, logger);
            
            
            // Act / Assert
            Assert.Throws<Exception>( () => decorator.GetOrCreate(key, typeof(AClass), options, factory));
            Assert.AreEqual(1, factoryCallCount);
        }
        
        [Test]
        public void GetOrCreate_ReturnsCachedValue()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var cachedValue = new AClass();
            var options = new CacheEntryOptions(TimeSpan.FromMilliseconds(1));
            var cache = Substitute.For<ICache>();
            cache.Get(key, typeof(AClass)).Returns(cachedValue);
            var logger = Substitute.For<ILogger<ExceptionHandlingCacheDecorator>>();
            var decorator = new ExceptionHandlingCacheDecorator(cache, logger);
            
            // Act
            var result = decorator.GetOrCreate(key, 
                typeof(AClass), 
                options, 
                _ => null );
            
            // Assert
            Assert.AreEqual(cachedValue, result);
        }
        
        [Test]
        public void Set_HandlesException()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var value = new AClass();
            var options = new CacheEntryOptions(TimeSpan.FromMilliseconds(1));
            var cache = Substitute.For<ICache>();
            cache.When(c => c.Set(key, value, options)).Do(_ => throw new Exception());
            var logger = Substitute.For<ILogger<ExceptionHandlingCacheDecorator>>();
            var decorator = new ExceptionHandlingCacheDecorator(cache, logger);
            
            // Act / Assert
            Assert.DoesNotThrow(() => decorator.Set(key, value, options));
        }
        
        [Test]
        public void SetAsync_HandlesException()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var value = new AClass();
            var options = new CacheEntryOptions(TimeSpan.FromMilliseconds(1));
            var cache = Substitute.For<ICache>();
            cache.SetAsync(key, value, options).Throws<Exception>();
            var logger = Substitute.For<ILogger<ExceptionHandlingCacheDecorator>>();
            var decorator = new ExceptionHandlingCacheDecorator(cache, logger);
            
            // Act / Assert
            Assert.DoesNotThrowAsync(() => decorator.SetAsync(key, value, options));
        }
        
        public class AClass
        {
        }
    }
}
