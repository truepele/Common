using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NUnit.Framework;
using Polly;
using Polly.Caching;
using Polly.Caching.Distributed;
using Polly.Caching.Memory;
using Polly.Caching.Serialization.Json;
using Polly.Interception.Caching;
using Polly.Registry;

namespace Common.Tests.Polly.Interception.Caching
{
    public class CachePolicyInterceptionTests
    {
        [Test]
        public async Task Foo()
        {
            var count1 = 0;
            var count2 = 0;
            var count3 = 0;
            
            var services = new ServiceCollection();
            services.AddMemoryCache()
                .AddSingleton<IAsyncCacheProvider, MemoryCacheProvider>()
                .AddSingleton<ISyncCacheProvider, MemoryCacheProvider>()
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
                .AddSingleton<IService, DelegatingService>()
                .AddCachePolicyInterception();
            
            var provider = services.BuildServiceProvider();
            var service = provider.GetRequiredService<IService>();
           
            // Act
            var result11 = await service.GetAsync(-1);
            var result12 = await service.GetAsync(-1);
            
            // Assert
            Assert.AreEqual(1, count1);
            Assert.AreEqual(result11, result12);
            Assert.DoesNotThrow(() => Guid.Parse(result11.Str));
        }
        
        
        [Test]
        public async Task Foo2()
        {
            // Arrange
            
            var count1 = 0;
            var count2 = 0;
            var count3 = 0;
            var count4 = 0;
            var dto1 = new Dto();
            var dto2 = new Dto();
            var str3 = Guid.NewGuid().ToString();
            var syncValue4 = Guid.NewGuid().ToString();

            var services = new ServiceCollection();
            services
                .AddMemoryCache()
                .AddSingleton<IAsyncCacheProvider, MemoryCacheProvider>()
                .AddSingleton<ISyncCacheProvider, MemoryCacheProvider>()
                
                .AddSingleton<Func<int, Task<Dto>>>(_ =>
                {
                    count1++;
                    return Task.FromResult(dto1);
                })
                .AddSingleton<Func<Task<Dto>>>(() =>
                {
                    count2++;
                    return Task.FromResult(dto2);
                })
                .AddSingleton<Func<int, Task<string>>>(_ =>
                {
                    count3++;
                    return Task.FromResult(str3);
                })
                .AddSingleton<Func<string>>(() =>
                {
                    count4++;
                    return syncValue4;
                })
                .AddSingleton<IService<int, Dto>, DelegatingService<int, Dto>>()
                
                .AddCachePolicyInterception();
            
            var provider = services.BuildServiceProvider();
            var service = provider.GetRequiredService<IService<int, Dto>>();
            
           
            // Act
            
            var result11 = await service.GetAsync(-1);
            var result12 = await service.GetAsync(-1);
            
            var result21 = await service.GetAsync();
            var result22 = await service.GetAsync();
            
            var result31 = await service.GetStringAsync(-1);
            var result32 = await service.GetStringAsync(-1);
            
            var result41 = service.GetString();
            var result42 = service.GetString();
           
            
            // Assert
            
            Assert.AreEqual(1, count1);
            Assert.AreEqual(result11, result12);
            Assert.AreEqual(dto1, result11);
            
            Assert.AreEqual(1, count2);
            Assert.AreEqual(result21, result22);
            Assert.AreEqual(dto2, result21);
            
            Assert.AreEqual(1, count3);
            Assert.AreEqual(result31, result32);
            Assert.AreEqual(str3, result31);
            
            Assert.AreEqual(1, count4);
            Assert.AreEqual(result41, result42);
            Assert.AreEqual(syncValue4, result41);

            Assert.DoesNotThrow(() => Guid.Parse(result11.Str));
        }
        
        
        [Test]
        public async Task Foo3()
        {
            // Arrange
            
            var services = new ServiceCollection();
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = "localhost"; // or whatever
                //options.InstanceName = "SampleInstance";
            });

            // Obtain a Newtonsoft.Json.JsonSerializerSettings defining any settings to use for serialization
            // (could alternatively be obtained from a factory by DI)
            var serializerSettings = new JsonSerializerSettings()
            {
                // Any configuration options
            };

            // Register a Polly cache provider for caching Dto entities, using the IDistributedCache instance and a Polly.Caching.Serialization.Json.JsonSerializer.
            // (ICacheItemSerializer<Dto, string> could alternatively be obtained from a factory by DI)

            services.AddSingleton(serviceProvider =>
                serviceProvider
                    .GetRequiredService<IDistributedCache>()
                    .AsAsyncCacheProvider<string>());
            //
            // services.AddSingleton(typeof(IAsyncCacheProvider<>), 
            //     typeof(AsyncSerializingCacheProvider<, string>));
            
            services.AddSingleton<IAsyncCacheProvider<Dto>>(serviceProvider =>
                serviceProvider
                    .GetRequiredService<IAsyncCacheProvider<string>>()
                    .WithSerializer<Dto, string>(
                        new JsonSerializer<Dto>(serializerSettings)
                    ));

            // Register a Polly cache policy for caching Dto entities, using that IDistributedCache instance.
            services.AddSingleton<IReadOnlyPolicyRegistry<string>, PolicyRegistry>((serviceProvider) =>
            {
                var registry = new PolicyRegistry
                {
                    {
                        "1",
                        Policy.CacheAsync<Dto>(serviceProvider.GetRequiredService<IAsyncCacheProvider<Dto>>(), TimeSpan.FromMinutes(60))
                    }
                };

                return registry;
            });
            var p = services.BuildServiceProvider();
            var reg = p.GetRequiredService<IReadOnlyPolicyRegistry<string>>();
            var policy = reg.Get<IAsyncPolicy<Dto>>("1");
            var cnt = 0;

            var sw = Stopwatch.StartNew();
            var r1 = await policy.ExecuteAsync(ctx =>
            {
                cnt++;
                return Task.FromResult(new Dto());
            }, new Context("12"));
            
            var r2 = await policy.ExecuteAsync(ctx =>
            {
                cnt++;
                return Task.FromResult(new Dto());
            }, new Context("12"));
            
            sw.Stop();
            Assert.True(cnt < 2);
            Assert.AreEqual(r1.Id, r2.Id);
        }
        

        public class Dto
        {
            public Guid Id { get; set; } = Guid.NewGuid();
            public string Str { get; set; } = Guid.NewGuid().ToString();
        }
        
        public interface IService
        {
            Task<Dto> GetAsync(int param);
            Task<Dto> GetAsync();
            Task<string> GetStringAsync(int param);
        }
        
        public class DelegatingService : IService
        {
            private readonly Func<int, Task<Dto>> _func;
            private readonly Func<Task<Dto>> _funcParamless;
            private readonly Func<int, Task<string>> _funcStr;

            public DelegatingService(Func<int, Task<Dto>> func, 
                Func<Task<Dto>> funcParamless,
                Func<int, Task<string>> funcStr)
            {
                _func = func ?? throw new ArgumentNullException(nameof(func));
                _funcParamless = funcParamless ?? throw new ArgumentNullException(nameof(funcParamless));
                _funcStr = funcStr ?? throw new ArgumentNullException(nameof(funcStr));
            }

            [CachePolicy(10)]
            public Task<Dto> GetAsync(int param)
            {
                return _func(param);
            }

            [CachePolicy(10, ExpirationType.Sliding)]
            public Task<Dto> GetAsync()
            {
                return _funcParamless();
            }

            [CachePolicy(int.MaxValue)]
            public Task<string> GetStringAsync(int param)
            {
                return _funcStr(param);
            }
        }

        public interface IService<in TParam, TResult>
        {
            Task<TResult> GetAsync(TParam param);
            Task<TResult> GetAsync();
            Task<string> GetStringAsync(TParam param);
            string GetString();
        }
        
        public class DelegatingService<TParam, TResult> : IService<TParam, TResult>
        {
            private readonly Func<TParam, Task<TResult>> _func;
            private readonly Func<Task<TResult>> _funcParamless;
            private readonly Func<TParam, Task<string>> _funcStr;
            private readonly Func<string> _syncFunc;

            public DelegatingService(Func<TParam, Task<TResult>> func, 
                Func<Task<TResult>> funcParamless,
                Func<TParam, Task<string>> funcStr,
                Func<string> syncFunc)
            {
                _func = func ?? throw new ArgumentNullException(nameof(func));
                _funcParamless = funcParamless ?? throw new ArgumentNullException(nameof(funcParamless));
                _funcStr = funcStr ?? throw new ArgumentNullException(nameof(funcStr));
                _syncFunc = syncFunc ?? throw new ArgumentNullException(nameof(syncFunc));
            }
        
            [CachePolicy(10)]
            public Task<TResult> GetAsync(TParam param)
            {
                return _func(param);
            }
        
            [CachePolicy(11, ExpirationType.Sliding)]
            public Task<TResult> GetAsync()
            {
                return _funcParamless();
            }
        
            [CachePolicy(-1, ExpirationType.NoExpiration)]
            public Task<string> GetStringAsync(TParam param)
            {
                return _funcStr(param);
            }
            
            [CachePolicy(-1, ExpirationType.NoExpiration)]
            public string GetString()
            {
                return _syncFunc();
            }
        }
    }
}