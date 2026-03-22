using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TenantHub.Data.Models;

namespace TenantHub.Data;

public class AdminDbContext : IdentityDbContext<ApplicationUser>
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantFeature> TenantFeatures => Set<TenantFeature>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("dbo");

        modelBuilder.Entity<Tenant>(e =>
        {
            e.HasIndex(t => t.Slug).IsUnique();
            e.HasMany(t => t.Features)
                .WithOne(f => f.Tenant)
                .HasForeignKey(f => f.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TenantFeature>(e =>
        {
            e.HasIndex(f => new { f.TenantId, f.FeatureName }).IsUnique();
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasIndex(a => a.OccurredAt);
        });
    }
}
