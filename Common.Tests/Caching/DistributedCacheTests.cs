using System;
using System.Text;
using System.Threading.Tasks;
using Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace Common.Tests.Caching
{
    [TestFixture]
    public class DistributedCacheTests
    {
        // Json exceptions
        [Test]
        public void GetOrCreateGeneric_DoesNotThrowJsonException()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            const string badJson = "{blah:\"\"";
            var msDistributedCache = Substitute.For<IDistributedCache>();
            msDistributedCache.Get(key).Returns(Encoding.UTF8.GetBytes(badJson));
            var expectedValue = Guid.NewGuid().ToString();
            var sut = new DistributedCache(msDistributedCache, Substitute.For<ILogger<DistributedCache>>());
            
            // Act
            var result = sut.GetOrCreate(key, new CacheEntryOptions(TimeSpan.MaxValue), k => expectedValue);
            
            //
            Assert.AreEqual(expectedValue, result);
        }
        
        [Test]
        public void GetOrCreate_DoesNotThrowJsonException()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            const string badJson = "{blah:\"\"";
            var msDistributedCache = Substitute.For<IDistributedCache>();
            msDistributedCache.Get(key).Returns(Encoding.UTF8.GetBytes(badJson));
            var expectedValue = Guid.NewGuid().ToString();
            var sut = new DistributedCache(msDistributedCache, Substitute.For<ILogger<DistributedCache>>());
            
            // Act
            var result = sut.GetOrCreate(key, 
                typeof(string), 
                new CacheEntryOptions(TimeSpan.MaxValue), 
                k => expectedValue);

            //
            Assert.AreEqual(expectedValue, result);
        }
        
        [Test]
        public async Task GetOrCreateAsync_DoesNotThrowJsonException()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            const string badJson = "{blah:\"\"";
            var msDistributedCache = Substitute.For<IDistributedCache>();
            msDistributedCache.GetAsync(key).Returns(Encoding.UTF8.GetBytes(badJson));
            var expectedValue = Guid.NewGuid().ToString();
            var sut = new DistributedCache(msDistributedCache, Substitute.For<ILogger<DistributedCache>>());
            
            // Act
            var result = await sut.GetOrCreateAsync(key,
                new CacheEntryOptions(TimeSpan.MaxValue), 
                k => Task.FromResult(expectedValue));

            //
            Assert.AreEqual(expectedValue, result);
        }
    }
}