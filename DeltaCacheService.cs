using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using VideoGameAPI.VideoGameModel;

namespace VideoGameAPI.Services
{
    public interface IDeltaCacheService
    {
        ActionResult<T> GetOrSetCache<T>(string cacheKey, T data, HttpContext context);
    }

    public class DeltaCacheService : IDeltaCacheService
    {
        private readonly IMemoryCache _cache;

        public DeltaCacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public ActionResult<T> GetOrSetCache<T>(string cacheKey, T data, HttpContext context)
        {
            var ifNoneMatch = context.Request.Headers.IfNoneMatch.ToString();
            var ifModifiedSince = context.Request.Headers.IfModifiedSince.ToString();

            if (_cache.TryGetValue(cacheKey, out CacheEntry<T>? cachedEntry) && cachedEntry != null)
            {
                if (!string.IsNullOrEmpty(ifNoneMatch) && ifNoneMatch == cachedEntry.ETag)
                {
                    return new StatusCodeResult(304);
                }

                if (
                    !string.IsNullOrEmpty(ifModifiedSince)
                    && DateTime.TryParse(ifModifiedSince, out var modifiedSinceDate)
                    && modifiedSinceDate >= cachedEntry.LastModified
                )
                {
                    return new StatusCodeResult(304);
                }

                context.Response.Headers.ETag = cachedEntry.ETag;
                context.Response.Headers.LastModified = cachedEntry.LastModified.ToString("R");
                return new OkObjectResult(cachedEntry.Data);
            }

            var cacheEntry = new CacheEntry<T>(data);
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(30))
                .SetPriority(CacheItemPriority.High)
                .SetSize(1);

            _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);

            context.Response.Headers.ETag = cacheEntry.ETag;
            context.Response.Headers.LastModified = cacheEntry.LastModified.ToString("R");
            return new OkObjectResult(cacheEntry.Data);
        }
    }
}
