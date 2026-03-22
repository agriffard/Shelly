using Microsoft.EntityFrameworkCore;
using TenantHub.Core.DTOs;
using TenantHub.Core.Services;
using TenantHub.Data.Models;

namespace TenantHub.Data.Services;

public class TenantManagementService(AdminDbContext db, IAuditService audit, IShellReloadService shellReload) : ITenantManagementService
{
    private static readonly string[] AllFeatures = ["Core", "Notes", "Tasks", "Announcements", "Media"];

    public async Task<PagedResult<TenantDto>> GetTenantsAsync(int page, int pageSize, string? search = null, CancellationToken ct = default)
    {
        var q = db.Tenants.AsNoTracking()
            .Where(t => t.Status != Core.DTOs.TenantStatus.Deleted);

        if (!string.IsNullOrEmpty(search))
            q = q.Where(t => t.Name.Contains(search) || t.Slug.Contains(search));

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TenantDto(
                t.Id, t.Name, t.Slug, t.Description,
                t.Status, t.CreatedAt, t.SuspendedAt,
                t.Features.Count(f => f.IsEnabled)))
            .ToListAsync(ct);

        return new PagedResult<TenantDto>(items, totalCount, page, pageSize);
    }

    public async Task<TenantDto?> GetTenantByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await db.Tenants.AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TenantDto(
                t.Id, t.Name, t.Slug, t.Description,
                t.Status, t.CreatedAt, t.SuspendedAt,
                t.Features.Count(f => f.IsEnabled)))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<TenantDto> CreateTenantAsync(CreateTenantRequest request, string actor, CancellationToken ct = default)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = request.Slug.ToLowerInvariant(),
            Description = request.Description,
            Status = Core.DTOs.TenantStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        // Create feature entries — Core always enabled, others disabled by default
        foreach (var feature in AllFeatures)
        {
            tenant.Features.Add(new TenantFeature
            {
                Id = Guid.NewGuid(),
                FeatureName = feature,
                IsEnabled = feature == "Core",
                UpdatedAt = DateTime.UtcNow
            });
        }

        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(ct);

        var enabledFeatures = tenant.Features
            .Where(f => f.IsEnabled)
            .Select(f => f.FeatureName)
            .ToList();

        await shellReload.AddShellAsync(tenant.Slug, enabledFeatures, ct);
        await audit.LogAsync(actor, "TenantCreated", "Tenant", tenant.Id.ToString(), $"Created tenant '{tenant.Name}' ({tenant.Slug})", ct);

        return new TenantDto(tenant.Id, tenant.Name, tenant.Slug, tenant.Description,
            tenant.Status, tenant.CreatedAt, tenant.SuspendedAt,
            enabledFeatures.Count);
    }

    public async Task<TenantDto> UpdateTenantAsync(Guid id, UpdateTenantRequest request, string actor, CancellationToken ct = default)
    {
        var tenant = await db.Tenants.Include(t => t.Features)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new KeyNotFoundException($"Tenant {id} not found");

        tenant.Name = request.Name;
        tenant.Description = request.Description;
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(actor, "TenantUpdated", "Tenant", id.ToString(), $"Updated tenant '{tenant.Name}'", ct);

        var enabledCount = tenant.Features.Count(f => f.IsEnabled);
        return new TenantDto(tenant.Id, tenant.Name, tenant.Slug, tenant.Description,
            tenant.Status, tenant.CreatedAt, tenant.SuspendedAt, enabledCount);
    }

    public async Task SetTenantStatusAsync(Guid id, Core.DTOs.TenantStatus status, string actor, CancellationToken ct = default)
    {
        var tenant = await db.Tenants.Include(t => t.Features)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new KeyNotFoundException($"Tenant {id} not found");

        tenant.Status = status;
        tenant.SuspendedAt = status == Core.DTOs.TenantStatus.Suspended ? DateTime.UtcNow : null;
        await db.SaveChangesAsync(ct);

        if (status == Core.DTOs.TenantStatus.Suspended)
        {
            await shellReload.RemoveShellAsync(tenant.Slug, ct);
        }
        else if (status == Core.DTOs.TenantStatus.Active)
        {
            var features = tenant.Features.Where(f => f.IsEnabled).Select(f => f.FeatureName).ToList();
            await shellReload.AddShellAsync(tenant.Slug, features, ct);
        }

        await audit.LogAsync(actor, $"TenantStatus{status}", "Tenant", id.ToString(), $"Set status to {status}", ct);
    }

    public async Task DeleteTenantAsync(Guid id, string actor, CancellationToken ct = default)
    {
        var tenant = await db.Tenants
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new KeyNotFoundException($"Tenant {id} not found");

        tenant.Status = Core.DTOs.TenantStatus.Deleted;
        await db.SaveChangesAsync(ct);

        await shellReload.RemoveShellAsync(tenant.Slug, ct);
        await audit.LogAsync(actor, "TenantDeleted", "Tenant", id.ToString(), $"Deleted tenant '{tenant.Name}'", ct);
    }

    public async Task ToggleFeatureAsync(Guid tenantId, string featureName, bool enabled, string actor, CancellationToken ct = default)
    {
        var tenant = await db.Tenants.Include(t => t.Features)
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new KeyNotFoundException($"Tenant {tenantId} not found");

        var feature = tenant.Features.FirstOrDefault(f => f.FeatureName == featureName);
        if (feature is null)
        {
            feature = new TenantFeature
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FeatureName = featureName,
                IsEnabled = enabled,
                UpdatedAt = DateTime.UtcNow
            };
            tenant.Features.Add(feature);
        }
        else
        {
            feature.IsEnabled = enabled;
            feature.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);

        var enabledFeatures = tenant.Features
            .Where(f => f.IsEnabled)
            .Select(f => f.FeatureName)
            .ToList();

        await shellReload.UpdateShellAsync(tenant.Slug, enabledFeatures, ct);

        var action = enabled ? "FeatureEnabled" : "FeatureDisabled";
        await audit.LogAsync(actor, action, "TenantFeature", $"{tenantId}:{featureName}", $"{featureName} {action.ToLowerInvariant()}", ct);
    }

    public async Task<IReadOnlyList<TenantFeatureDto>> GetTenantFeaturesAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await db.TenantFeatures
            .AsNoTracking()
            .Where(f => f.TenantId == tenantId)
            .Select(f => new TenantFeatureDto(f.Id, f.FeatureName, f.IsEnabled, f.UpdatedAt))
            .ToListAsync(ct);
    }
}
