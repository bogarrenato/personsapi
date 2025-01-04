using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using personsapi.Interfaces;
using StackExchange.Redis;

namespace personsapi.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDatabase _database;
        private readonly ILogger<RedisCacheService> _logger;

        public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
        {
            _database = redis.GetDatabase();
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var cachedValue = await _database.StringGetAsync(key);

            if (cachedValue.IsNullOrEmpty)
            {
                _logger.LogInformation("Cache miss for key: {Key}", key);
                return default;
            }

            _logger.LogInformation("Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(cachedValue!);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            var serializedData = JsonSerializer.Serialize(value);
            await _database.StringSetAsync(key, serializedData, expiration);
            _logger.LogInformation("Cache set for key: {Key}", key);
        }

        public async Task InvalidateAsync(params string[] keys)
        {
            if (keys == null || keys.Length == 0) return;

            var tasks = new List<Task>();

            foreach (var key in keys)
            {
                if (key.Contains("*"))
                {
                    tasks.Add(InvalidateByPatternAsync(key));
                }
                else
                {
                    tasks.Add(_database.KeyDeleteAsync(key));
                    _logger.LogInformation("Cache invalidated for key: {Key}", key);
                }
            }

            await Task.WhenAll(tasks);
        }

        private async Task InvalidateByPatternAsync(string pattern)
        {
            var cursor = 0L;
            do
            {
                var scan = await _database.ExecuteAsync("SCAN", cursor.ToString(), "MATCH", pattern, "COUNT", "100");
                var entries = (RedisResult[])scan[1];
                cursor = (long)scan[0];

                if (entries.Length > 0)
                {
                    var keys = entries.Select(e => (RedisKey)e.ToString()).ToArray();
                    await _database.KeyDeleteAsync(keys);
                    _logger.LogInformation("Invalidated {Count} keys matching pattern: {Pattern}", keys.Length, pattern);
                }
            }
            while (cursor != 0);
        }

        public async Task<bool> ExistsAsync(string key)
        {
            return await _database.KeyExistsAsync(key);
        }
    }
}