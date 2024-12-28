# bootRequestPerformaceDotnetDelta
# Delta Cache Service

## Tổng Quan

Delta Cache Service là một giải pháp caching thông minh cho ASP.NET Core APIs, sử dụng HTTP caching headers (ETag, Last-Modified) để tối ưu hiệu suất và băng thông.

## Cấu Trúc

## Thành Phần

### 1. CacheEntry<T>

public class CacheEntry<T>
{
public T Data { get; set; }
public string ETag { get; set; }
public DateTime LastModified { get; set; }
}

### 2. IDeltaCacheService

public interface IDeltaCacheService
{
ActionResult<T> GetOrSetCache<T>(string cacheKey, T data, HttpContext context);
}

### 3. Cấu Hình trong Program.cs

// Đăng ký service
builder.Services.AddScoped<IDeltaCacheService, DeltaCacheService>();
// Cấu hình cache
builder.Services.AddMemoryCache(options =>
{
options.CompactionPercentage = 0.2;
options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
});

## Cách Sử Dụng

### 1. Inject Service

public class MyController : ControllerBase
{
private readonly IDeltaCacheService deltaCacheService;
public MyController(IDeltaCacheService deltaCacheService)
{
deltaCacheService = deltaCacheService;
}

### 2. Implement trong Action

[HttpGet]
[OutputCache(Duration = 300)]
[ResponseCache(CacheProfileName = "Default")]
public ActionResult<T> GetData()
{
return \_deltaCacheService.GetOrSetCache("CacheKey", data, HttpContext);
}

## Cache Flow

1. **First Request**:

   - Generate ETag và LastModified
   - Cache data với metadata
   - Return full response

2. **Subsequent Requests**:
   - Check If-None-Match và If-Modified-Since headers
   - Return 304 Not Modified nếu data không thay đổi
   - Return new data nếu có thay đổi

## Cache Options

var cacheEntryOptions = new MemoryCacheEntryOptions()
.SetSlidingExpiration(TimeSpan.FromMinutes(5))
.SetAbsoluteExpiration(TimeSpan.FromMinutes(30))
.SetPriority(CacheItemPriority.High)
.SetSize(1);

## Lợi Ích

1. **Hiệu Suất**

   - Giảm bandwidth với 304 responses
   - Giảm server load
   - Tối ưu client caching

2. **Nhất Quán**

   - Validation hai lớp với ETag và LastModified
   - Tránh stale data
   - Đảm bảo phiên bản chính xác

3. **Mở Rộng**
   - Generic type support
   - Dễ tích hợp
   - Configurable

## Best Practices

1. **Cache Keys**

   - Sử dụng key có ý nghĩa
   - Tránh conflict
   - Namespace appropriately

2. **Timing**

   - UTC cho timestamps
   - Cấu hình expiration phù hợp
   - Consider data volatility

3. **Headers**
   - Set appropriate cache headers
   - Include Vary headers if needed
   - Configure max-age properly

## Ví Dụ Implementation

Xem `VideoGameController.cs` để tham khảo implementation cụ thể

## Monitoring

- Monitor cache hit/miss ratio
- Track bandwidth savings
- Watch memory usage
- Log cache operations

## Dependencies

- Microsoft.AspNetCore.Mvc
- Microsoft.Extensions.Caching.Memory
- System.Runtime.Caching

## Contributing
