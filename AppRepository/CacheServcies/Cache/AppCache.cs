using Microsoft.Extensions.Caching.Memory;

namespace Services.CacheServcies.Cache
{

    public abstract class AppCache<T>
    {
        protected readonly IMemoryCache _cache;

        internal readonly TimeSpan _saveTime = TimeSpan.FromMinutes(10);


        public AppCache(IMemoryCache memoryCache)
        {
            _cache = memoryCache;
        }
    }

}
