using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace personsapi.Interfaces
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task InvalidateAsync(params string[] keys);
        Task<bool> ExistsAsync(string key);
    }
}