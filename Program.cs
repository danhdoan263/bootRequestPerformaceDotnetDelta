using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using VideoGameAPI.Controllers;
using VideoGameAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Tối ưu Kestrel - điều chỉnh giới hạn
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxConcurrentConnections = 1000; // Tăng lên để xử lý nhiều request hơn
    serverOptions.Limits.MaxConcurrentUpgradedConnections = 1000;
    serverOptions.Limits.MaxRequestBodySize = 52428800; // 50MB
    serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(60);
    serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(60);
});

// 2. Tối ưu Controller và JSON
builder
    .Services.AddControllers(options =>
    {
        options.SuppressAsyncSuffixInActionNames = false;
        options.CacheProfiles.Add(
            "Default",
            new CacheProfile
            {
                Duration = 300, // Tăng thời gian cache lên 5 phút
                Location = ResponseCacheLocation.Any,
                VaryByHeader = "Accept,Accept-Encoding", // Thêm vary by header
            }
        );
    })
    .AddApplicationPart(typeof(Program).Assembly)
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.WriteIndented = false;
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System
            .Text
            .Json
            .Serialization
            .JsonIgnoreCondition
            .WhenWritingNull;
    });

// 3. Tối ưu Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json", "application/xml", "text/plain" }
    );
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

// 4. Cấu hình Memory Cache
builder.Services.AddMemoryCache(options =>
{
    options.CompactionPercentage = 0.2;
    options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
});

// 5. Cấu hình Output Cache
builder.Services.AddOutputCache(options =>
{
    options.DefaultExpirationTimeSpan = TimeSpan.FromMinutes(5); // Tăng lên 5 phút
    options.SizeLimit = 200_000;
    options.AddBasePolicy(builder =>
        builder
            .Expire(TimeSpan.FromMinutes(5))
            .SetVaryByHeader("Accept", "Accept-Encoding") // Thêm vary headers
            .Cache()
    );
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Video Game API", Version = "v1" });
});

builder.Services.AddScoped<IDeltaCacheService, DeltaCacheService>();

var app = builder.Build();

// 6. Middleware theo thứ tự tối ưu
app.UseResponseCompression();
app.UseOutputCache();

// 7. Cấu hình HTTP Headers tối ưu
app.Use(
    async (context, next) =>
    {
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.XFrameOptions = "SAMEORIGIN";
        context.Response.GetTypedHeaders().CacheControl =
            new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(30),
                MustRevalidate = true,
                NoTransform = true,
            };
        await next();
    }
);

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c =>
    {
        c.RouteTemplate = "openapi/{documentName}.json";
    });
    app.MapScalarApiReference();
}

app.Run();
