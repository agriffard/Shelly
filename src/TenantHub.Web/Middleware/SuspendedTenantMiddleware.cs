using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TenantHub.Core.DTOs;
using TenantHub.Data;

namespace TenantHub.Web.Middleware;

public class SuspendedTenantMiddleware(RequestDelegate next, IMemoryCache cache)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.TrimStart('/');
        var firstSegment = path?.Split('/').FirstOrDefault();

        if (!string.IsNullOrEmpty(firstSegment) && !IsKnownPrefix(firstSegment))
        {
            var cacheKey = $"tenant_status:{firstSegment}";
            if (!cache.TryGetValue(cacheKey, out TenantStatus? status))
            {
                var db = context.RequestServices.GetRequiredService<AdminDbContext>();
                var tenant = await db.Tenants
                    .AsNoTracking()
                    .Where(t => t.Slug == firstSegment)
                    .Select(t => new { t.Status })
                    .FirstOrDefaultAsync();

                status = tenant?.Status;
                cache.Set(cacheKey, status, CacheDuration);
            }

            if (status == TenantStatus.Suspended)
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Tenant is currently suspended.");
                return;
            }
        }

        await next(context);
    }

    private static bool IsKnownPrefix(string segment) =>
        segment is "admin" or "account" or "_framework" or "css" or "_blazor" or "_content";
}
