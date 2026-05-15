using CShells.Lifecycle;
using Microsoft.Extensions.Caching.Memory;
using TenantHub.Core.Services;
using TenantHub.Web.Middleware;

namespace TenantHub.Web.Services;

public class CShellsReloadService(IShellRegistry shellRegistry, IMemoryCache cache) : IShellReloadService
{
    public async Task AddShellAsync(string slug, IReadOnlyList<string> features, CancellationToken ct = default)
    {
        await shellRegistry.ActivateAsync(slug, ct);
        SuspendedTenantMiddleware.InvalidateCache(cache, slug);
    }

    public async Task UpdateShellAsync(string slug, IReadOnlyList<string> features, CancellationToken ct = default)
    {
        if (shellRegistry.GetActive(slug) is null)
            await shellRegistry.ActivateAsync(slug, ct);
        else
            await shellRegistry.ReloadAsync(slug, ct);

        SuspendedTenantMiddleware.InvalidateCache(cache, slug);
    }

    public async Task RemoveShellAsync(string slug, CancellationToken ct = default)
    {
        if (await shellRegistry.GetBlueprintAsync(slug, ct) is not null)
            await shellRegistry.UnregisterBlueprintAsync(slug, ct);

        SuspendedTenantMiddleware.InvalidateCache(cache, slug);
    }
}
