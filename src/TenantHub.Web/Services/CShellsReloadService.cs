using CShells;
using CShells.Hosting;
using CShells.Management;
using Microsoft.Extensions.Caching.Memory;
using TenantHub.Core.Services;
using TenantHub.Web.Components;
using TenantHub.Web.Middleware;

namespace TenantHub.Web.Services;

public class CShellsReloadService(IShellManager shellManager, IMemoryCache cache) : IShellReloadService
{
    private static IShellHost GetShellHost() =>
        (AppServices.RootProvider.GetService(typeof(IShellHost)) as IShellHost)!;

    public async Task AddShellAsync(string slug, IReadOnlyList<string> features, CancellationToken ct = default)
    {
        var settings = BuildSettings(slug, features);
        await GetShellHost().EvictShellAsync(new ShellId(slug));
        await shellManager.AddShellAsync(settings, ct);
        SuspendedTenantMiddleware.InvalidateCache(cache, slug);
    }

    public async Task UpdateShellAsync(string slug, IReadOnlyList<string> features, CancellationToken ct = default)
    {
        // Use ReloadShellAsync which re-fetches from DatabaseShellSettingsProvider.
        // The DB has already been updated before this call.
        await GetShellHost().EvictShellAsync(new ShellId(slug));
        try
        {
            await shellManager.ReloadShellAsync(new ShellId(slug), ct);
        }
        catch (InvalidOperationException)
        {
            // Shell not found by provider — fall back to add
            var settings = BuildSettings(slug, features);
            await shellManager.AddShellAsync(settings, ct);
        }
        SuspendedTenantMiddleware.InvalidateCache(cache, slug);
    }

    public async Task RemoveShellAsync(string slug, CancellationToken ct = default)
    {
        await GetShellHost().EvictShellAsync(new ShellId(slug));
        await shellManager.RemoveShellAsync(new ShellId(slug), ct);
        SuspendedTenantMiddleware.InvalidateCache(cache, slug);
    }

    private static ShellSettings BuildSettings(string slug, IReadOnlyList<string> features)
    {
        return new ShellSettings(new ShellId(slug), features)
        {
            ConfigurationData = new Dictionary<string, object>
            {
                ["WebRouting:Path"] = slug,
                ["TenantSchema"] = slug
            }
        };
    }
}
