using CShells.Lifecycle;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using TenantHub.Data;

namespace TenantHub.Web.Components;

public static class AppServices
{
    public static IServiceProvider RootProvider { get; set; } = default!;
}

public abstract class TenantComponentBase : ComponentBase, IAsyncDisposable
{
    [Parameter] public string Slug { get; set; } = "";

    protected virtual string? RequiredFeature => null;

    protected bool TenantExists { get; private set; }
    protected bool IsFeatureEnabled { get; private set; }

    private IShellScope? _shellScope;
    private HashSet<string>? _enabledFeatures;

    protected override async Task OnParametersSetAsync()
    {
        if (_shellScope is not null)
        {
            await _shellScope.DisposeAsync();
            _shellScope = null;
        }

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
        {
            await EnsureShellScopeAsync();
            await LoadDataAsync();
        }
    }

    /// <summary>
    /// Override in pages to load data. Called after tenant and feature checks pass.
    /// </summary>
    protected virtual Task LoadDataAsync() => Task.CompletedTask;

    private async Task EnsureShellScopeAsync()
    {
        if (_shellScope is not null || string.IsNullOrEmpty(Slug))
            return;

        var shellRegistry = AppServices.RootProvider.GetRequiredService<IShellRegistry>();
        var shell = await shellRegistry.GetOrActivateAsync(Slug);
        _shellScope = shell.BeginScope();
    }

    protected IServiceProvider? ShellServices => _shellScope?.ServiceProvider;

    protected bool HasFeature(string featureName) =>
        _enabledFeatures?.Contains(featureName) ?? false;

    protected T? GetShellService<T>() where T : class =>
        ShellServices?.GetService(typeof(T)) as T;

    public async ValueTask DisposeAsync()
    {
        if (_shellScope is not null)
        {
            await _shellScope.DisposeAsync();
            _shellScope = null;
        }
    }
}
