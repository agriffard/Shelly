using CShells;
using CShells.Management;
using TenantHub.Core.Services;

namespace TenantHub.Web.Services;

public class CShellsReloadService(IShellManager shellManager) : IShellReloadService
{
    public async Task AddShellAsync(string slug, IReadOnlyList<string> features, CancellationToken ct = default)
    {
        var settings = BuildSettings(slug, features);
        await shellManager.AddShellAsync(settings, ct);
    }

    public async Task UpdateShellAsync(string slug, IReadOnlyList<string> features, CancellationToken ct = default)
    {
        var settings = BuildSettings(slug, features);
        await shellManager.UpdateShellAsync(settings, ct);
    }

    public async Task RemoveShellAsync(string slug, CancellationToken ct = default)
    {
        await shellManager.RemoveShellAsync(new ShellId(slug), ct);
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
