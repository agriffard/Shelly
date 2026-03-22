using CShells;
using CShells.Hosting;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using TenantHub.Data;

namespace TenantHub.Web.Components;

public static class AppServices
{
    public static IServiceProvider RootProvider { get; set; } = default!;
}

public abstract class TenantComponentBase : ComponentBase, IDisposable
{
    [Parameter] public string Slug { get; set; } = "";

    protected virtual string? RequiredFeature => null;

    protected bool TenantExists { get; private set; }
    protected bool IsFeatureEnabled { get; private set; }

    private IServiceScope? _shellScope;
    private HashSet<string>? _enabledFeatures;

    protected override async Task OnParametersSetAsync()
    {
        if (!string.IsNullOrEmpty(Slug))
        {
            using var scope = AppServices.RootProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

            var tenant = await db.Tenants
                .AsNoTracking()
                .Where(t => t.Slug == Slug && t.Status != Core.DTOs.TenantStatus.Deleted)
                .Select(t => new { t.Id })
                .FirstOrDefaultAsync();

            TenantExists = tenant is not null;

            if (TenantExists)
            {
                var features = await db.TenantFeatures
                    .AsNoTracking()
                    .Where(f => f.Tenant.Slug == Slug && f.IsEnabled)
                    .Select(f => f.FeatureName)
                    .ToListAsync();
                _enabledFeatures = features.ToHashSet();
            }
        }

        IsFeatureEnabled = TenantExists
            && (RequiredFeature is null || (_enabledFeatures?.Contains(RequiredFeature) ?? false));

        if (IsFeatureEnabled)
            await LoadDataAsync();
    }

    /// <summary>
    /// Override in pages to load data. Called after tenant and feature checks pass.
    /// </summary>
    protected virtual Task LoadDataAsync() => Task.CompletedTask;

    private ShellContext? GetShellContext()
    {
        if (string.IsNullOrEmpty(Slug)) return null;
        try
        {
            var shellHost = AppServices.RootProvider.GetService(typeof(IShellHost)) as IShellHost;
            return shellHost?.GetShell(new ShellId(Slug));
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }

    protected IServiceProvider? ShellServices
    {
        get
        {
            if (_shellScope is not null)
                return _shellScope.ServiceProvider;

            var shell = GetShellContext();
            if (shell is null) return null;

            _shellScope = shell.ServiceProvider.CreateScope();
            return _shellScope.ServiceProvider;
        }
    }

    protected bool HasFeature(string featureName) =>
        _enabledFeatures?.Contains(featureName) ?? false;

    protected T? GetShellService<T>() where T : class =>
        ShellServices?.GetService(typeof(T)) as T;

    public void Dispose()
    {
        _shellScope?.Dispose();
        _shellScope = null;
    }
}
