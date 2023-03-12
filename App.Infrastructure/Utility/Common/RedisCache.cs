using App.Domain.Config;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace App.Infrastructure.Utility.Common
{
    public interface IRedisCache
    {
        Task<bool> Set<T>(string key, T obj);

        Task<T> Get<T>(string key);

        Task<bool> SetString(string key, string value);

        Task<string> GetString(string key);
    }

    public class RedisCache : IRedisCache

    {
        private readonly CacheSettingConfig _cachesettingConfig;

        public RedisCache(IOptions<CacheSettingConfig> cachesettingConfig)
        {
            _cachesettingConfig = cachesettingConfig.Value;
        }

        public async Task<T> Get<T>(string key)
        {
            var stringValue = await GetString(key);
            if (string.IsNullOrEmpty(stringValue))
                return default(T);
            return JsonConvert.DeserializeObject<T>(stringValue);
        }

        public async Task<bool> Set<T>(string key, T obj)
        {
            if (string.IsNullOrEmpty(key) || obj == null)
                return false;

            return await SetString(key, JsonConvert.SerializeObject(obj));
        }

        public async Task<string> GetString(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;
            var cache = GetCacheDb();
            return await cache.StringGetAsync(key);
        }

        public async Task<bool> SetString(string key, string value)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                return false;
            var cache = GetCacheDb();
            await cache.StringSetAsync(key, value, new TimeSpan(0, 15, 0));

            return true;
        }

        private IDatabase GetCacheDb()
        {
            var lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
            {
                string cacheConnection = _cachesettingConfig.CacheConnection;
                return ConnectionMultiplexer.Connect(cacheConnection);
            });

            return lazyConnection.Value.GetDatabase();
        }
    }
}