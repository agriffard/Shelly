using CShells;
using CShells.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace TenantHub.Data.Providers;

public class DatabaseShellSettingsProvider(IServiceProvider rootProvider) : IShellSettingsProvider
{
    public async Task<IEnumerable<ShellSettings>> GetShellSettingsAsync(CancellationToken cancellationToken = default)
    {
        using var scope = rootProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

        var tenants = await db.Tenants
            .AsNoTracking()
            .Where(t => t.Status == Core.DTOs.TenantStatus.Active)
            .Include(t => t.Features.Where(f => f.IsEnabled))
            .ToListAsync(cancellationToken);

        return tenants.Select(MapToShellSettings);
    }

    public async Task<ShellSettings?> GetShellSettingsAsync(ShellId shellId, CancellationToken cancellationToken = default)
    {
        using var scope = rootProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

        var tenant = await db.Tenants
            .AsNoTracking()
            .Include(t => t.Features.Where(f => f.IsEnabled))
            .FirstOrDefaultAsync(t => t.Slug == shellId.Name, cancellationToken);

        if (tenant is null || tenant.Status != Core.DTOs.TenantStatus.Active)
            return null;

        return MapToShellSettings(tenant);
    }

    private static ShellSettings MapToShellSettings(Models.Tenant tenant)
    {
        var features = tenant.Features
            .Where(f => f.IsEnabled)
            .Select(f => f.FeatureName)
            .ToList();

        if (!features.Contains("Core"))
            features.Insert(0, "Core");

        return new ShellSettings(new ShellId(tenant.Slug), features)
        {
            ConfigurationData = new Dictionary<string, object>
            {
                ["WebRouting:Path"] = tenant.Slug,
                ["TenantSchema"] = tenant.Slug
            }
        };
    }
}
