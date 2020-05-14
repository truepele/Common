using System;
using System.Threading;
using System.Threading.Tasks;
using Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using MemoryCache = Microsoft.Extensions.Caching.Memory.MemoryCache;

namespace Common.Tests.Caching
{
    [TestFixture]
    public class DistributedCacheTests
    {
        [Test]
        public void Get_DoesNotThrow_WhenNoCachedValueForValueType()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var msDistributedCache = Substitute.For<IDistributedCache>();
            msDistributedCache.Get(key).Returns((byte[])null);
            const int expectedValue = default(int);
            var sut = new DistributedCache(msDistributedCache, Substitute.For<ILogger<DistributedCache>>());
            
            // Act
            var result = sut.Get<int>(key);
            
            // Assert
            Assert.AreEqual(expectedValue, result);
        }

        [Test]
        public async Task GetOrCreateAsync_ReturnsCachedValue()
        {
            // Arrange
            
            var key = Guid.NewGuid().ToString();
            var cachedValue = new AClass();
            Func<string, Task<AClass>> factory = _ => Task.FromResult(new AClass());
            var options = new CacheEntryOptions(TimeSpan.FromMilliseconds(1));

            var cache = new FakeDistributedCache(new MemoryCache(new MemoryCacheOptions()));
            cache.SetString(key, JsonConvert.SerializeObject(cachedValue));
            
            var logger = Substitute.For<ILogger<DistributedCache>>();
            var sut = new DistributedCache(cache, logger);
            
            
            // Act
            var result = await sut.GetOrCreateAsync(key, options, factory);
            
            // Assert
            Assert.AreEqual(cachedValue.Value, result.Value);
        }

        [Test]
        public async Task GetOrCreateAsync_ReturnsIntCachedValue()
        {
            // Arrange
            
            var key = Guid.NewGuid().ToString();
            const int cachedValue = 1234;
            Func<string, Task<int>> factory = _ => Task.FromResult(4321);
            var options = new CacheEntryOptions(TimeSpan.FromMilliseconds(1));

            var cache = new FakeDistributedCache(new MemoryCache(new MemoryCacheOptions()));
            cache.SetString(key, JsonConvert.SerializeObject(cachedValue));
            
            var logger = Substitute.For<ILogger<DistributedCache>>();
            var sut = new DistributedCache(cache, logger);
            
            
            // Act
            var result = await sut.GetOrCreateAsync(key, options, factory);
            
            // Assert
            Assert.AreEqual(cachedValue, result);
        }
        
        [Test]
        public async Task GetOrCreateAsync_ReturnsBoolCachedValue()
        {
            // Arrange
            
            var key = Guid.NewGuid().ToString();
            const bool cachedValue = true;
            Func<string, Task<bool>> factory = _ => Task.FromResult(false);
            var options = new CacheEntryOptions(TimeSpan.FromMilliseconds(1));

            var cache = new FakeDistributedCache(new MemoryCache(new MemoryCacheOptions()));
            cache.SetString(key, JsonConvert.SerializeObject(cachedValue));
            
            var logger = Substitute.For<ILogger<DistributedCache>>();
            var sut = new DistributedCache(cache, logger);
            
            
            // Act
            var result = await sut.GetOrCreateAsync(key, options, factory);
            
            // Assert
            Assert.AreEqual(cachedValue, result);
        }

        [Test]
        public async Task GetOrCreateAsync_ReturnsFactoryValue()
        {
            // Arrange
            
            var key = Guid.NewGuid().ToString();
            var factoryValue = new AClass();
            Func<string, Task<AClass>> factory = _ => Task.FromResult(factoryValue);
            var options = new CacheEntryOptions(TimeSpan.FromMilliseconds(1));

            var cache = Substitute.For<IDistributedCache>();
            cache.GetAsync(key, Arg.Any<CancellationToken>()).Returns((byte[])null);
            
            var logger = Substitute.For<ILogger<DistributedCache>>();
            var sut = new DistributedCache(cache, logger);
            
            
            // Act
            var result = await sut.GetOrCreateAsync(key, options, factory);
            
            // Assert
            Assert.AreEqual(factoryValue.Value, result.Value);
        }
        
        [Test]
        public void GetOrCreateGeneric_ReturnsCachedValue()
        {
            // Arrange
            
            var key = Guid.NewGuid().ToString();
            var cachedValue = new AClass();
            Func<string, AClass> factory = _ => new AClass();
            var options = new CacheEntryOptions(TimeSpan.FromMilliseconds(1));

            var cache = new FakeDistributedCache(new MemoryCache(new MemoryCacheOptions()));
            cache.SetString(key, JsonConvert.SerializeObject(cachedValue));
            
            var logger = Substitute.For<ILogger<DistributedCache>>();
            var sut = new DistributedCache(cache, logger);
            
            
            // Act
            var result = sut.GetOrCreate(key, options, factory);
            
            // Assert
            Assert.AreEqual(cachedValue.Value, result.Value);
        }
        
        [Test]
        public void GetOrCreateGeneric_ReturnsIntCachedValue()
        {
            // Arrange
            
            var key = Guid.NewGuid().ToString();
            const int cachedValue = 1234;
            Func<string, int> factory = _ => 4321;
            var options = new CacheEntryOptions(TimeSpan.FromMilliseconds(1));

            var cache = new FakeDistributedCache(new MemoryCache(new MemoryCacheOptions()));
            cache.SetString(key, JsonConvert.SerializeObject(cachedValue));
            
            var logger = Substitute.For<ILogger<DistributedCache>>();
            var sut = new DistributedCache(cache, logger);
            
            
            // Act
            var result = sut.GetOrCreate(key, options, factory);
            
            // Assert
            Assert.AreEqual(cachedValue, result);
        }
        
        [Test]
        public void GetOrCreateGeneric_ReturnsBoolCachedValue()
        {
            // Arrange
            
            var key = Guid.NewGuid().ToString();
            const bool cachedValue = true;
            Func<string, bool> factory = _ => false;
            var options = new CacheEntryOptions(TimeSpan.FromMilliseconds(1));

            var cache = new FakeDistributedCache(new MemoryCache(new MemoryCacheOptions()));
            cache.SetString(key, JsonConvert.SerializeObject(cachedValue));
            
            var logger = Substitute.For<ILogger<DistributedCache>>();
            var sut = new DistributedCache(cache, logger);
            
            
            // Act
            var result = sut.GetOrCreate(key, options, factory);
            
            // Assert
            Assert.AreEqual(cachedValue, result);
        }

        [Test]
        public void GetOrCreateGeneric_ReturnsFactoryValue()
        {
            // Arrange
            
            var key = Guid.NewGuid().ToString();
            var factoryValue = new AClass();
            Func<string, AClass> factory = _ => factoryValue;
            var options = new CacheEntryOptions(TimeSpan.FromMilliseconds(1));

            var cache = Substitute.For<IDistributedCache>();
            cache.Get(key).Returns((byte[])null);
            
            var logger = Substitute.For<ILogger<DistributedCache>>();
            var sut = new DistributedCache(cache, logger);
            
            
            // Act
            var result = sut.GetOrCreate(key, options, factory);
            
            // Assert
            Assert.AreEqual(factoryValue.Value, result.Value);
        }

        [Test]
        public void GetOrCreate_ReturnsCachedValue()
        {
            // Arrange
            
            var key = Guid.NewGuid().ToString();
            var cachedValue = new AClass();
            Func<string, AClass> factory = _ => new AClass();
            var options = new CacheEntryOptions(TimeSpan.FromMilliseconds(1));

            var cache = new FakeDistributedCache(new MemoryCache(new MemoryCacheOptions()));
            cache.SetString(key, JsonConvert.SerializeObject(cachedValue));
            
            var logger = Substitute.For<ILogger<DistributedCache>>();
            var sut = new DistributedCache(cache, logger);
            
            
            // Act
            var result = (AClass) sut.GetOrCreate(key, typeof(AClass), options, factory);
            
            // Assert
            Assert.AreEqual(cachedValue.Value, result.Value);
        }
        
        [Test]
        public void GetOrCreate_ReturnsIntCachedValue()
        {
            // Arrange
            
            var key = Guid.NewGuid().ToString();
            const int cachedValue = 1234;
            Func<string, object> factory = _ => 4321;
            var options = new CacheEntryOptions(TimeSpan.FromMilliseconds(1));

            var cache = new FakeDistributedCache(new MemoryCache(new MemoryCacheOptions()));
            cache.SetString(key, JsonConvert.SerializeObject(cachedValue));
            
            var logger = Substitute.For<ILogger<DistributedCache>>();
            var sut = new DistributedCache(cache, logger);
            
            
            // Act
            var result = sut.GetOrCreate(key, typeof(int), options, factory);
            
            // Assert
            Assert.AreEqual(cachedValue, result);
        }
        
        [Test]
        public void GetOrCreate_ReturnsBoolCachedValue()
        {
            // Arrange
            
            var key = Guid.NewGuid().ToString();
            const bool cachedValue = true;
            Func<string, object> factory = _ => false;
            var options = new CacheEntryOptions(TimeSpan.FromMilliseconds(1));

            var cache = new FakeDistributedCache(new MemoryCache(new MemoryCacheOptions()));
            cache.SetString(key, JsonConvert.SerializeObject(cachedValue));
            
            var logger = Substitute.For<ILogger<DistributedCache>>();
            var sut = new DistributedCache(cache, logger);
            
            
            // Act
            var result = sut.GetOrCreate(key, typeof(bool), options, factory);
            
            // Assert
            Assert.AreEqual(cachedValue, result);
        }

        [Test]
        public void GetOrCreate_ReturnsFactoryValue()
        {
            // Arrange
            
            var key = Guid.NewGuid().ToString();
            var factoryValue = new AClass();
            Func<string, AClass> factory = _ => factoryValue;
            var options = new CacheEntryOptions(TimeSpan.FromMilliseconds(1));

            var cache = Substitute.For<IDistributedCache>();
            cache.Get(key).Returns((byte[])null);
            
            var logger = Substitute.For<ILogger<DistributedCache>>();
            var sut = new DistributedCache(cache, logger);
            
            
            // Act
            var result = (AClass)sut.GetOrCreate(key, typeof(AClass), options, factory);
            
            // Assert
            Assert.AreEqual(factoryValue.Value, result.Value);
        }

        [Test]
        public async Task GetAsync_ReturnsCachedValue()
        {
            // Arrange
            
            var key = Guid.NewGuid().ToString();
            var cachedValue = new AClass();

            var cache = new FakeDistributedCache(new MemoryCache(new MemoryCacheOptions()));
            cache.SetString(key, JsonConvert.SerializeObject(cachedValue));
            
            var logger = Substitute.For<ILogger<DistributedCache>>();
            var sut = new DistributedCache(cache, logger);
            
            
            // Act
            var result = await sut.GetAsync<AClass>(key);
            
            // Assert
            Assert.AreEqual(cachedValue.Value, result.Value);
        }

        [Test]
        public async Task GetAsync_ReturnsIntCachedValue()
        {
            // Arrange
            
            var key = Guid.NewGuid().ToString();
            const int cachedValue = 1234;

            var cache = new FakeDistributedCache(new MemoryCache(new MemoryCacheOptions()));
            cache.SetString(key, JsonConvert.SerializeObject(cachedValue));
            
            var logger = Substitute.For<ILogger<DistributedCache>>();
            var sut = new DistributedCache(cache, logger);
            
            
            // Act
            var result = await sut.GetAsync<int>(key);
            
            // Assert
            Assert.AreEqual(cachedValue, result);
        }
        
        [Test]
        public async Task GetAsync_ReturnsBoolCachedValue()
        {
            // Arrange
            
            var key = Guid.NewGuid().ToString();
            const bool cachedValue = true;

            var cache = new FakeDistributedCache(new MemoryCache(new MemoryCacheOptions()));
            cache.SetString(key, JsonConvert.SerializeObject(cachedValue));
            
            var logger = Substitute.For<ILogger<DistributedCache>>();
            var sut = new DistributedCache(cache, logger);
            
            
            // Act
            var result = await sut.GetAsync<bool>(key);
            
            // Assert
            Assert.AreEqual(cachedValue, result);
        }
        
        [Test]
        public async Task GetIfCachedAsync_ReturnsCachedValue()
        {
            // Arrange
            
            var key = Guid.NewGuid().ToString();
            var cachedValue = new AClass();

            var cache = new FakeDistributedCache(new MemoryCache(new MemoryCacheOptions()));
            cache.SetString(key, JsonConvert.SerializeObject(cachedValue));
            
            var logger = Substitute.For<ILogger<DistributedCache>>();
            var sut = new DistributedCache(cache, logger);
            
            
            // Act
            var result = await sut.GetIfCachedAsync<AClass>(key);
            
            // Assert
            Assert.True(result.wasInCache);
            Assert.AreEqual(cachedValue.Value, result.value.Value);
        }
        
        [Test]
        public async Task GetIfCachedAsync_ReturnsFalse_WhenNoCachedValue()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var cache = new FakeDistributedCache(new MemoryCache(new MemoryCacheOptions()));
            var logger = Substitute.For<ILogger<DistributedCache>>();
            var sut = new DistributedCache(cache, logger);
            
            
            // Act
            var result = await sut.GetIfCachedAsync<AClass>(key);
            
            // Assert
            Assert.False(result.wasInCache);
            Assert.IsNull(result.value);
        }
        
        [Test]
        public void GetIfCached_ReturnsCachedValue()
        {
            // Arrange
            
            var key = Guid.NewGuid().ToString();
            var cachedValue = new AClass();

            var cache = new FakeDistributedCache(new MemoryCache(new MemoryCacheOptions()));
            cache.SetString(key, JsonConvert.SerializeObject(cachedValue));
            
            var logger = Substitute.For<ILogger<DistributedCache>>();
            var sut = new DistributedCache(cache, logger);
            
            
            // Act
            var wasCached = sut.GetIfCached<AClass>(key, out var result);
            
            // Assert
            Assert.True(wasCached);
            Assert.AreEqual(cachedValue.Value, result.Value);
        }
        
        [Test]
        public void GetIfCached_ReturnsFalse_WhenNoCachedValue()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var cache = new FakeDistributedCache(new MemoryCache(new MemoryCacheOptions()));
            var logger = Substitute.For<ILogger<DistributedCache>>();
            var sut = new DistributedCache(cache, logger);

            // Act
            var wasCached = sut.GetIfCached<AClass>(key, out var result);
            
            // Assert
            Assert.False(wasCached);
            Assert.IsNull(result);
        }

        
        public class AClass
        {
            public string Value { get; set; } = Guid.NewGuid().ToString();
        }
    }
}
