using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Caching;
using Caching.Interception;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using StackExchange.Redis;

namespace Common.Tests.Caching.Interception
{
    [Ignore("Do not run on build")]
    [TestFixture]
    public class CacheInterceptorRedisIntegrationTests
    {
        private readonly string _redisConnectionString;

        public CacheInterceptorRedisIntegrationTests()
        {
            _redisConnectionString = "localhost:6379";
        }
        
        
        [TearDown]
        public async Task TearDown()
        {
            var connectionMultiplexer = ConnectionMultiplexer.Connect(_redisConnectionString);
            var redisDb = connectionMultiplexer.GetDatabase();
            var server = connectionMultiplexer.GetServer(_redisConnectionString);
           
            foreach (var key in server.Keys(pattern: "*DelegatingService*"))
            {
                await redisDb.KeyDeleteAsync(key);
            }
        }

        [Test]
        public async Task GenericTypeInterceptedCorrectly_RedisCache()
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
                .AddSingleton<Func<int, Task<Dto>>>(async _ =>
                {
                    count1++;
                    await Task.Delay(1);
                    return dto1;
                })
                .AddSingleton<Func<Task<Dto>>>(async () =>
                {
                    count2++;
                    await Task.Delay(1);
                    return dto2;
                })
                .AddSingleton<Func<int, Task<string>>>(async _ =>
                {
                    count3++;
                    await Task.Delay(1);
                    return str3;
                })
                .AddSingleton<Func<string>>(() =>
                {
                    count4++;
                    return syncValue4;
                })
                .AddSingleton<IService<int, Dto>, DelegatingService<int, Dto>>()
                .AddLogging()
                .InterceptWithStackExchangeRedisCacheByAttribute(options =>
                {
                    options.Configuration = _redisConnectionString;
                    options.InstanceName = "myredis";
                });

          
            var provider = services.BuildServiceProvider();
            var service = provider.GetRequiredService<IService<int, Dto>>();


            // Act

            var result11 = await service.GetAsync(-1);

            var sw = Stopwatch.StartNew();
            var result12 = await service.GetAsync(-1);
            sw.Stop();
            Debug.WriteLine(sw.Elapsed);
            Console.WriteLine(sw.Elapsed);
            
            var result21 = await service.GetAsync();
            var result22 = await service.GetAsync();
            
            var result31 = await service.GetStringAsync(-1);
            var result32 = await service.GetStringAsync(-1);
            
            var result41 = service.GetString();
            var result42 = service.GetString();
            
            
            // Assert
            
            Assert.AreEqual(1, count1);
            Assert.AreEqual(result11.Id, result12.Id);
            Assert.AreEqual(dto1.Id, result11.Id);
            
            Assert.AreEqual(1, count2);
            Assert.AreEqual(result21.Id, result22.Id);
            Assert.AreEqual(dto2.Id, result21.Id);
            
            Assert.AreEqual(1, count3);
            Assert.AreEqual(result31, result32);
            Assert.AreEqual(str3, result31);
            
            Assert.True(count4 == 1);
            Assert.AreEqual(result41, result42);
            Assert.AreEqual(syncValue4, result41);

            await Task.Delay(1000);
            var connectionMultiplexer = ConnectionMultiplexer.Connect(_redisConnectionString);
            var server = connectionMultiplexer.GetServer(_redisConnectionString);

            Assert.AreEqual(2, server.Keys(pattern: "*DelegatingService*").Count());
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
        
            [Cache(1000)]
            public Task<TResult> GetAsync(TParam param)
            {
                return _func(param);
            }
        
            [Cache(1000, ExpirationType.Sliding)]
            public Task<TResult> GetAsync()
            {
                return _funcParamless();
            }
        
            [Cache(-1, ExpirationType.NoExpiration)]
            public Task<string> GetStringAsync(TParam param)
            {
                return _funcStr(param);
            }
            
            [Cache(-1, ExpirationType.NoExpiration)]
            public string GetString()
            {
                return _syncFunc();
            }
        }
    }
}
