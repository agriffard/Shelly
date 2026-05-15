using CShells;
using CShells.AspNetCore.Features;
using CShells.Features;
using CShells.Lifecycle;
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
            options.ConfigureWarnings(w =>
            {
                // Schema differs at runtime vs migration time
                w.Ignore(RelationalEventId.PendingModelChangesWarning);
                // Suppress "Failed executing DbCommand" logs for CREATE TABLE/INDEX
                // that fail because objects already exist — these are expected and caught
                //w.Ignore(RelationalEventId.CommandError);
            });
        });

        services.AddShellInitializer<TenantDbMigrationHandler>(LifecyclePhase.Start, order: 100);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints, IHostEnvironment? environment)
    {
    }
}
