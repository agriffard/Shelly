using CShells.Lifecycle;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TenantHub.Core.Services;
using TenantHub.Data;

namespace TenantHub.Features.Handlers;

public class TenantDbMigrationHandler(IServiceProvider serviceProvider, ILogger<TenantDbMigrationHandler> logger) : IShellInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();

        var tenantService = scope.ServiceProvider.GetRequiredService<ICurrentTenantService>();
        if (tenantService.TenantSlug == "Default")
            return;

        var dbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var schema = tenantService.TenantSchema;

        // Check if tables already exist for this tenant schema
        var tableCount = await dbContext.Database.SqlQueryRaw<int>(
            $"SELECT COUNT(*) AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{schema}'"
        ).FirstOrDefaultAsync(cancellationToken);

        if (tableCount > 0)
        {
            logger.LogDebug("Tenant schema '{Schema}' already has {Count} table(s), skipping creation", schema, tableCount);
            return;
        }

        logger.LogInformation("Creating tables for new tenant schema '{Schema}'", schema);

        // Create schema
#pragma warning disable EF1002
        await dbContext.Database.ExecuteSqlRawAsync(
            $"IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'{schema}') EXEC('CREATE SCHEMA [{schema}]')",
            cancellationToken);
#pragma warning restore EF1002

        // Create tables from the runtime model
        var creator = dbContext.GetService<IRelationalDatabaseCreator>();
        var createScript = creator.GenerateCreateScript();

        var statements = createScript
            .Split(["GO"], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s));

        foreach (var statement in statements)
        {
#pragma warning disable EF1002
            await dbContext.Database.ExecuteSqlRawAsync(statement, cancellationToken);
#pragma warning restore EF1002
        }

        logger.LogInformation("Tables created for tenant schema '{Schema}'", schema);
    }
}
