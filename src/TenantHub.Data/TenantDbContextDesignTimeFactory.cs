using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TenantHub.Core.Services;

namespace TenantHub.Data;

public class TenantDbContextDesignTimeFactory : IDesignTimeDbContextFactory<TenantDbContext>
{
    public TenantDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=TenantHub_Design;Trusted_Connection=True");

        return new TenantDbContext(optionsBuilder.Options, new DesignTimeTenantService());
    }

    private sealed class DesignTimeTenantService : ICurrentTenantService
    {
        public string TenantSlug => "tenant_template";
        public string TenantSchema => "tenant_template";
    }
}
