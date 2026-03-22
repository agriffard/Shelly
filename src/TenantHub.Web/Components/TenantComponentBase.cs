using CShells;
using CShells.Hosting;
using Microsoft.AspNetCore.Components;

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
    /// <summary>
    /// Subclasses must bind this to the {Slug} route parameter.
    /// </summary>
    [Parameter] public string Slug { get; set; } = "";

    private IServiceScope? _shellScope;

    protected IServiceProvider? ShellServices
    {
        get
        {
            if (_shellScope is not null)
                return _shellScope.ServiceProvider;

            if (string.IsNullOrEmpty(Slug))
                return null;

            try
            {
                var shellHost = AppServices.RootProvider.GetService(typeof(IShellHost)) as IShellHost;
                if (shellHost is null) return null;

                var shell = shellHost.GetShell(new ShellId(Slug));
                _shellScope = shell.ServiceProvider.CreateScope();
                return _shellScope.ServiceProvider;
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }
    }

    protected T? GetShellService<T>() where T : class =>
        ShellServices?.GetService(typeof(T)) as T;

    public void Dispose()
    {
        _shellScope?.Dispose();
        _shellScope = null;
    }
}
