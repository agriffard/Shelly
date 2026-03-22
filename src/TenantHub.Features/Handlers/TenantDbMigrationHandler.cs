using CShells.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TenantHub.Core.Services;
using TenantHub.Data;

namespace TenantHub.Features.Handlers;

public class TenantDbMigrationHandler(IServiceProvider serviceProvider, ILogger<TenantDbMigrationHandler> logger) : IShellActivatedHandler
{
    public async Task OnActivatedAsync(CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();

        var tenantService = scope.ServiceProvider.GetRequiredService<ICurrentTenantService>();
        if (tenantService.TenantSlug == "Default")
        {
            logger.LogInformation("Skipping table creation for Default shell");
            return;
        }

        var dbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var schema = tenantService.TenantSchema;
        logger.LogInformation("Creating tables for tenant schema '{Schema}'", schema);

        // Ensure schema exists
        if (!string.IsNullOrEmpty(schema))
        {
            var sql = $"IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'{schema}') EXEC('CREATE SCHEMA [{schema}]')";
#pragma warning disable EF1002
            await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
#pragma warning restore EF1002
        }

        // Use the RelationalDatabaseCreator to create tables from the runtime model.
        // EnsureCreatedAsync skips if the DB exists, but CreateTablesAsync always creates
        // the tables defined in the model (in the tenant's schema).
        var creator = dbContext.GetService<IRelationalDatabaseCreator>();
        try
        {
            await creator.CreateTablesAsync(cancellationToken);
            logger.LogInformation("Tables created for tenant schema '{Schema}'", schema);
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 2714)
        {
            // "There is already an object named '...' in the database" — tables already exist
            logger.LogInformation("Tables already exist for tenant schema '{Schema}'", schema);
        }
    }
}
