using CShells;
using CShells.AspNetCore.Features;
using CShells.Features;
using CShells.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TenantHub.Core.Services;
using TenantHub.Data;
using TenantHub.Data.Services;
using TenantHub.Features.Handlers;

namespace TenantHub.Features;

[ShellFeature("Core", DisplayName = "Core", Description = "Core tenant services — always enabled")]
public class CoreFeature(ShellSettings shellSettings) : IWebShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        var schema = shellSettings.ConfigurationData.TryGetValue("TenantSchema", out var s)
            ? s?.ToString() ?? shellSettings.Id.Name
            : shellSettings.Id.Name;

        var slug = shellSettings.Id.Name;

        services.AddSingleton<ICurrentTenantService>(new CurrentTenantService(slug, schema));

        services.AddDbContext<TenantDbContext>((sp, options) =>
        {
            var connString = sp.GetRequiredService<IConfiguration>()
                .GetConnectionString("DefaultConnection");
            options.UseSqlServer(connString, sql =>
            {
                // Put migrations history table in the tenant's schema so each tenant
                // tracks its own migration state independently
                sql.MigrationsHistoryTable("__EFMigrationsHistory", schema);
            });
            // Schema differs at runtime (tenant slug) vs migration time ("tenant_template")
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        services.AddSingleton<IShellActivatedHandler, TenantDbMigrationHandler>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints, IHostEnvironment? environment)
    {
        // Only map tenant endpoints for real tenant shells, not the Default shell
        if (shellSettings.Id.Name == "Default")
            return;

        // Tenant home
        endpoints.MapGet("", (ICurrentTenantService tenant) =>
            Results.Ok(new { tenant = tenant.TenantSlug, status = "active", features = shellSettings.EnabledFeatures }));
    }
}
