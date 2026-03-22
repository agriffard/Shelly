using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TenantHub.Core.Services;
using TenantHub.Data.Models;

namespace TenantHub.Data;

public class TenantDbContext : DbContext
{
    internal readonly string _schema;

    public TenantDbContext(DbContextOptions<TenantDbContext> options, ICurrentTenantService tenantService)
        : base(options)
    {
        _schema = tenantService.TenantSchema;
    }

    public DbSet<Note> Notes => Set<Note>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<FileRecord> FileRecords => Set<FileRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(_schema);

        modelBuilder.Entity<Note>(e =>
        {
            e.HasIndex(n => n.CreatedAt);
        });

        modelBuilder.Entity<TaskItem>(e =>
        {
            e.HasIndex(t => t.AssignedTo);
            e.HasIndex(t => t.DueDate);
        });

        modelBuilder.Entity<Announcement>(e =>
        {
            e.HasIndex(a => a.ExpiresAt);
            e.HasIndex(a => a.IsPinned);
        });

        modelBuilder.Entity<FileRecord>(e =>
        {
            e.HasIndex(f => f.UploadedAt);
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Use a custom model cache key so each tenant schema gets its own cached model.
        // Without this, EF Core caches the model from the first tenant and reuses it
        // for all tenants, causing all tenants to use the first tenant's schema.
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
    }
}

/// <summary>
/// Ensures EF Core builds a separate model per schema, preventing cross-tenant model reuse.
/// </summary>
internal class TenantModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        if (context is TenantDbContext tenantContext)
        {
            return (context.GetType(), tenantContext._schema, designTime);
        }
        return (context.GetType(), designTime);
    }

    public object Create(DbContext context)
        => Create(context, false);
}
