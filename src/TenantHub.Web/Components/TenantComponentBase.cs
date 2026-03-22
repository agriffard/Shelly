using CShells;
using CShells.Hosting;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using TenantHub.Data;

namespace TenantHub.Web.Components;

/// <summary>
/// Holds a reference to the root IServiceProvider so Blazor components
/// can access IShellHost regardless of which DI scope they're in.
/// </summary>
public static class AppServices
{
    public static IServiceProvider RootProvider { get; set; } = default!;
}

/// <summary>
/// Base class for tenant Blazor components that resolves services from the correct
/// CShells shell DI container based on the Slug route parameter.
/// </summary>
public abstract class TenantComponentBase : ComponentBase, IDisposable
{
    [Parameter] public string Slug { get; set; } = "";

    /// <summary>
    /// Override to specify which feature this page requires.
    /// </summary>
    protected virtual string? RequiredFeature => null;

    protected bool IsFeatureEnabled { get; private set; } = true;

    private IServiceScope? _shellScope;
    private HashSet<string>? _enabledFeatures;

    protected override async Task OnParametersSetAsync()
    {
        // Always read features from DB — the source of truth
        if (!string.IsNullOrEmpty(Slug))
        {
            using var scope = AppServices.RootProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
            var features = await db.TenantFeatures
                .AsNoTracking()
                .Where(f => f.Tenant.Slug == Slug && f.IsEnabled)
                .Select(f => f.FeatureName)
                .ToListAsync();
            _enabledFeatures = features.ToHashSet();
        }

        IsFeatureEnabled = RequiredFeature is null
            || (_enabledFeatures?.Contains(RequiredFeature) ?? false);
    }

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
