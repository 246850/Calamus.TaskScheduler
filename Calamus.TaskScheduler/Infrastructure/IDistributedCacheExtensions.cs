using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Calamus.TaskScheduler.Infrastructure
{
    public static class IDistributedCacheExtensions
    {
        /// <summary>
        /// 设置缓存值 T 对象 json序列化
        /// </summary>
        /// <param name="distributedCache"></param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expiry">单位：秒</param>
        public static Task SetAsync<T>(this IDistributedCache distributedCache, string key, T value, long expiry, CancellationToken token = default(CancellationToken)) where T : class
            => SetAsync<T>(distributedCache, key, value, new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromSeconds(expiry) }, token);
        /// <summary>
        /// 设置缓存值 T 对象 json序列化
        /// </summary>
        /// <param name="distributedCache"></param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public static Task SetAsync<T>(this IDistributedCache distributedCache, string key, T value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
            where T : class
        {
            var json = JsonSerializer.Serialize(value);
            return distributedCache.SetStringAsync(key, json, options, token);
        }
        /// <summary>
        /// 获取缓存值 并转换 为 T 对象 - json序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="distributedCache"></param>
        /// <param name="key">键</param>
        /// <returns></returns>
        public static async Task<T> GetAsync<T>(this IDistributedCache distributedCache, string key, CancellationToken token = default(CancellationToken))
            where T : class
        {
            var json = await distributedCache.GetStringAsync(key, token);
            if (string.IsNullOrWhiteSpace(json)) return default(T);

            return JsonSerializer.Deserialize<T>(json);
        }
        /// <summary>
        /// 存在直接返回，不存在先创建缓存再返回
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="distributedCache"></param>
        /// <param name="key"></param>
        /// <param name="valueFactory"></param>
        /// <param name="expiry">单位：秒</param>
        /// <returns></returns>
        public static async Task<T> GetOrAddAsync<T>(this IDistributedCache distributedCache, string key, Func<T> valueFactory, long expiry, CancellationToken token = default(CancellationToken))
            where T : class
        {
            T result = await GetAsync<T>(distributedCache, key);

            if (result == null)
            {
                T item = valueFactory();
                await SetAsync(distributedCache, key, item, new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromSeconds(expiry) }, token);
                return item;
            }

            return result;
        }
        /// <summary>
        /// 存在直接返回，不存在先创建缓存再返回
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="distributedCache"></param>
        /// <param name="key"></param>
        /// <param name="valueFactory"></param>
        /// <param name="expiry">单位：秒</param>
        /// <returns></returns>
        public static async Task<T> GetOrAddAsync<T>(this IDistributedCache distributedCache, string key, Func<Task<T>> valueFactory, long expiry, CancellationToken token = default(CancellationToken))
            where T : class
        {
            T result = await GetAsync<T>(distributedCache, key);

            if (result == null)
            {
                T item = await valueFactory();
                await SetAsync(distributedCache, key, item, new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromSeconds(expiry) }, token);
                return item;
            }

            return result;
        }
    }
}
