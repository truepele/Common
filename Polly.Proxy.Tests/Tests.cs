using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;

namespace Polly.Proxy.Tests
{
    public class Tests
    {
        public interface IService<TEntity> where TEntity : class
        {
            Task<TEntity> GetEntityAsync(string query);
            TEntity GetEntity(string query);
        }
        
        public class Entity
        {
            public Guid Id { get; set; } = Guid.NewGuid();
        }
        
        public class AnException : Exception { }
        
        
        [Test]
        public async Task WithPolicy_Applies_Async_Retry_Policy_To_Async_Method()
        {
            // Arrange

            const int failCount = 2;
            var retriedCount = 0;
            var policy = Policy
                .Handle<AnException>()
                .WaitAndRetryAsync(100,
                retryAttempt  =>
                {
                    retriedCount = retryAttempt;
                    return TimeSpan.FromSeconds(Math.Pow(0.5, retryAttempt));
                }
            );
            
            var entity = new Entity();
            var originalService = Substitute.For<IService<Entity>>();
            var callCount = 0;
            
            var sw = Stopwatch.StartNew();
            originalService.GetEntityAsync(Arg.Any<string>()).Returns(async ci =>
            {
                await Task.Delay(1);
                Console.WriteLine(sw.Elapsed);
                if (callCount++ < failCount)
                {
                    throw new AnException();
                }

                return entity;
            });

            var proxy = originalService.WithPolicy(policy);
            
            
            // Act

            var result = await proxy.GetEntityAsync("");
            
            
            // Assert
            
            Assert.AreEqual(failCount, retriedCount);
            Assert.AreEqual(entity, result);
        }
        
        
        [Test]
        public void WithPolicy_Proxy_Fails_After_Retry()
        {
            // Arrange

            const int failCount = 10;
            const int retryCount = 2;
            var retriedCount = 0;
            var policy = Policy
                .Handle<AnException>()
                .WaitAndRetryAsync(retryCount,
                    retryAttempt  =>
                    {
                        retriedCount = retryAttempt;
                        return TimeSpan.FromSeconds(Math.Pow(0.5, retryAttempt));
                    }
                );
            
            var originalService = Substitute.For<IService<Entity>>();
            var callCount = 0;
            
            var sw = Stopwatch.StartNew();
            originalService.GetEntityAsync(Arg.Any<string>()).Returns(async ci =>
            {
                await Task.Delay(1);
                Console.WriteLine(sw.Elapsed);
                if (callCount++ < failCount)
                {
                    throw new AnException();
                }

                return new Entity();
            });

            var proxy = originalService.WithPolicy(policy);
            
            
            // Act / Assert

            Assert.ThrowsAsync<AnException>(() => proxy.GetEntityAsync(""));
            Assert.AreEqual(retryCount, retriedCount);
        }
        
        
        [Test]
        public void WithPolicy_Applies_Async_Retry_Policy_To_Sync_Method()
        {
            // Arrange

            const int failCount = 2;
            var retriedCount = 0;
            var policy = Policy
                .Handle<AnException>()
                .WaitAndRetryAsync(100,
                    retryAttempt =>
                    {
                        retriedCount = retryAttempt;
                        return TimeSpan.FromSeconds(Math.Pow(0.5, retryAttempt));
                    });
            
            var entity = new Entity();
            var originalService = Substitute.For<IService<Entity>>();
            var callCount = 0;
            
            var sw = Stopwatch.StartNew();
            originalService.GetEntity(Arg.Any<string>()).Returns(ci =>
            {
                Console.WriteLine(sw.Elapsed);
                if (callCount++ < failCount)
                {
                    throw new AnException();
                }

                return entity;
            });

            var proxy = originalService.WithPolicy(policy);
            
            
            // Act

            var result = proxy.GetEntity("");
            
            
            // Assert
            
            Assert.AreEqual(failCount, retriedCount);
            Assert.AreEqual(entity, result);
        }
    }
}