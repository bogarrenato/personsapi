
namespace personsapi.Attributes
{
    /// <summary>
    /// Attribute for caching action results
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CacheResultAttribute : Attribute
    {
        public string CacheKey { get; }
        public int Duration { get; }

        public CacheResultAttribute(string cacheKey, int duration = -1)
        {
            CacheKey = cacheKey;
            Duration = duration;
        }
    }

    /// <summary>
    /// Attribute for invalidating cache entries
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class InvalidateCacheAttribute : Attribute
    {
        public string[] CacheKeys { get; }

        public InvalidateCacheAttribute(params string[] cacheKeys)
        {
            CacheKeys = cacheKeys;
        }
    }
}