using Microsoft.EntityFrameworkCore;
using TenantHub.Core.Services;
using TenantHub.Data.Models;

namespace TenantHub.Data;

public class TenantDbContext : DbContext
{
    private readonly string _schema;

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
}
