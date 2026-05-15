using CShells;
using CShells.Lifecycle;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace TenantHub.Data.Providers;

public class DatabaseShellSettingsProvider(IServiceProvider rootProvider) : IShellBlueprintProvider
{
    private const string DefaultShellName = "Default";

    public async Task<ProvidedBlueprint> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.Equals(name, DefaultShellName, StringComparison.OrdinalIgnoreCase))
            return new ProvidedBlueprint(new DatabaseShellBlueprint(CreateDefaultShellSettings()), null);

        using var scope = rootProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

        var tenant = await db.Tenants
            .AsNoTracking()
            .Include(t => t.Features.Where(f => f.IsEnabled))
            .FirstOrDefaultAsync(t => t.Slug == name, cancellationToken);

        if (tenant is null || tenant.Status != Core.DTOs.TenantStatus.Active)
            throw new ShellBlueprintNotFoundException(name);

        return new ProvidedBlueprint(MapToBlueprint(tenant), null);
    }

    public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.Equals(name, DefaultShellName, StringComparison.OrdinalIgnoreCase))
            return true;

        using var scope = rootProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

        return await db.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.Slug == name && t.Status == Core.DTOs.TenantStatus.Active, cancellationToken);
    }

    public async Task<BlueprintPage> ListAsync(BlueprintListQuery query, CancellationToken cancellationToken = default)
    {
        query.EnsureValid();

        using var scope = rootProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

        var tenants = await db.Tenants
            .AsNoTracking()
            .Where(t => t.Status == Core.DTOs.TenantStatus.Active)
            .Select(t => t.Slug)
            .ToListAsync(cancellationToken);

        var allNames = tenants
            .Append(DefaultShellName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        if (!string.IsNullOrWhiteSpace(query.NamePrefix))
            allNames = allNames.Where(n => n.StartsWith(query.NamePrefix, StringComparison.Ordinal)).ToList();

        if (!string.IsNullOrWhiteSpace(query.Cursor))
            allNames = allNames.Where(n => string.CompareOrdinal(n, query.Cursor) > 0).ToList();

        var hasMore = allNames.Count > query.Limit;
        var pageNames = hasMore ? allNames.Take(query.Limit).ToList() : allNames;

        var items = pageNames
            .Select(name => new BlueprintSummary(
                Name: name,
                SourceId: nameof(DatabaseShellSettingsProvider),
                Mutable: false,
                Metadata: new Dictionary<string, string>()))
            .ToList();

        var nextCursor = hasMore ? pageNames[^1] : null;
        return new BlueprintPage(items, nextCursor);
    }

    private static IShellBlueprint MapToBlueprint(Models.Tenant tenant) =>
        new DatabaseShellBlueprint(MapToShellSettings(tenant));

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

    private static ShellSettings CreateDefaultShellSettings() =>
        new(new ShellId(DefaultShellName), ["Core"]);

    private sealed class DatabaseShellBlueprint(ShellSettings settings) : IShellBlueprint
    {
        public string Name => settings.Id.Name;

        public IReadOnlyDictionary<string, string> Metadata { get; } = new Dictionary<string, string>();

        public Task<ShellSettings> ComposeAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(settings);
    }
}
