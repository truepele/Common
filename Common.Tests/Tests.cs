using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using Polly.Caching.Memory;
using Polly.Interception;

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

            var proxy = originalService.InterceptWithPolicy(policy);
            
            
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

            var proxy = originalService.InterceptWithPolicy(policy);
            
            
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

            var proxy = originalService.InterceptWithPolicy(policy);
            
            
            // Act

            var result = proxy.GetEntity("");
            
            
            // Assert
            
            Assert.AreEqual(failCount, retriedCount);
            Assert.AreEqual(entity, result);
        }

        //[Test]
        public async Task Foo()
        {
            var count1 = 0;
            var count2 = 0;
            var count3 = 0;
            
            var services = new ServiceCollection();
            services.AddMemoryCache()
                .AddSingleton<Caching.IAsyncCacheProvider, MemoryCacheProvider>()
                .AddSingleton<Func<int, Task<Dto>>>(_ =>
                {
                    count1++;
                    return Task.FromResult(new Dto());
                })
                .AddSingleton<Func<Task<Dto>>>(() =>
                    {
                        count2++;
                        return Task.FromResult(new Dto());
                    })
                .AddSingleton<Func<int, Task<string>>>(_ =>
                {
                    count3++;
                    return Task.FromResult(Guid.NewGuid().ToString());
                })
                .AddSingleton(typeof(IService<,>), typeof(DelegatingService<,>));
               // .Decorate<>();
            
            var provider = services.BuildServiceProvider();

            var policy = Policy.CacheAsync(provider.GetService<Caching.IAsyncCacheProvider>(), TimeSpan.MaxValue);

            var service = provider.GetRequiredService<IService<int, Dto>>();
            
            service = service.InterceptWithPolicy(policy);
            
            // Act
            var result11 = await service.GetAsync(-1);
            var result12 = await service.GetAsync(-1);
            
            // Assert
            Assert.AreEqual(1, count1);
            Assert.AreEqual(result11, result12);
            Assert.DoesNotThrow(() => Guid.Parse(result11.Str));
        }

        public class Dto
        {
            public Guid Id { get; set; } = Guid.NewGuid();
            public string Str { get; set; } = Guid.NewGuid().ToString();
        }

        public interface IService<in TParam, TResult>
        {
            Task<TResult> GetAsync(TParam param);
            Task<TResult> GetAsync();
            Task<string> GetStringAsync(TParam param);
        }
        
        public class DelegatingService<TParam, TResult> : IService<TParam, TResult>
        {
            private readonly Func<TParam, Task<TResult>> _func;
            private readonly Func<Task<TResult>> _funcParamless;
            private readonly Func<TParam, Task<string>> _funcStr;

            public DelegatingService(Func<TParam, Task<TResult>> func, 
                Func<Task<TResult>> funcParamless,
                Func<TParam, Task<string>> funcStr)
            {
                _func = func ?? throw new ArgumentNullException(nameof(func));
                _funcParamless = funcParamless ?? throw new ArgumentNullException(nameof(funcParamless));
                _funcStr = funcStr ?? throw new ArgumentNullException(nameof(funcStr));
            }

            public Task<TResult> GetAsync(TParam param)
            {
                return _func(param);
            }

            public Task<TResult> GetAsync()
            {
                return _funcParamless();
            }

            public Task<string> GetStringAsync(TParam param)
            {
                return _funcStr(param);
            }
        }
    }
}