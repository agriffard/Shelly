using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TenantHub.Core.DTOs;
using TenantHub.Core.Services;
using TenantHub.Data;
using TenantHub.Data.Services;

namespace TenantHub.Tests;

public class TenantManagementServiceTests
{
    private static (AdminDbContext db, TenantManagementService svc) CreateService(string dbName)
    {
        var options = new DbContextOptionsBuilder<AdminDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var db = new AdminDbContext(options);

        var auditMock = new Mock<IAuditService>();
        var shellReloadMock = new Mock<IShellReloadService>();

        var svc = new TenantManagementService(db, auditMock.Object, shellReloadMock.Object);
        return (db, svc);
    }

    [Fact]
    public async Task CreateTenantAsync_PersistsTenantWithCoreEnabled()
    {
        var (db, svc) = CreateService(nameof(CreateTenantAsync_PersistsTenantWithCoreEnabled));

        var result = await svc.CreateTenantAsync(
            new CreateTenantRequest("Acme", "acme", "Test tenant"), "admin");

        Assert.Equal("Acme", result.Name);
        Assert.Equal("acme", result.Slug);
        Assert.Equal(TenantStatus.Active, result.Status);

        var tenant = await db.Tenants.Include(t => t.Features).FirstAsync();
        Assert.Contains(tenant.Features, f => f.FeatureName == "Core" && f.IsEnabled);
    }

    [Fact]
    public async Task ToggleFeatureAsync_EnablesFeature()
    {
        var (db, svc) = CreateService(nameof(ToggleFeatureAsync_EnablesFeature));

        var created = await svc.CreateTenantAsync(
            new CreateTenantRequest("Test", "test", null), "admin");

        await svc.ToggleFeatureAsync(created.Id, "Notes", true, "admin");

        var features = await svc.GetTenantFeaturesAsync(created.Id);
        Assert.True(features.First(f => f.FeatureName == "Notes").IsEnabled);
    }

    [Fact]
    public async Task SetTenantStatusAsync_SuspendsTenant()
    {
        var (db, svc) = CreateService(nameof(SetTenantStatusAsync_SuspendsTenant));

        var created = await svc.CreateTenantAsync(
            new CreateTenantRequest("Test", "test", null), "admin");

        await svc.SetTenantStatusAsync(created.Id, TenantStatus.Suspended, "admin");

        var tenant = await svc.GetTenantByIdAsync(created.Id);
        Assert.NotNull(tenant);
        Assert.Equal(TenantStatus.Suspended, tenant.Status);
        Assert.NotNull(tenant.SuspendedAt);
    }

    [Fact]
    public async Task GetTenantsAsync_SearchByName()
    {
        var (db, svc) = CreateService(nameof(GetTenantsAsync_SearchByName));

        await svc.CreateTenantAsync(new CreateTenantRequest("Alpha Corp", "alpha", null), "admin");
        await svc.CreateTenantAsync(new CreateTenantRequest("Beta Inc", "beta", null), "admin");

        var result = await svc.GetTenantsAsync(1, 50, "Alpha");

        Assert.Single(result.Items);
        Assert.Equal("Alpha Corp", result.Items[0].Name);
    }

    [Fact]
    public async Task DeleteTenantAsync_SetsStatusToDeleted()
    {
        var (db, svc) = CreateService(nameof(DeleteTenantAsync_SetsStatusToDeleted));

        var created = await svc.CreateTenantAsync(
            new CreateTenantRequest("Test", "test", null), "admin");

        await svc.DeleteTenantAsync(created.Id, "admin");

        var tenant = await db.Tenants.FindAsync(created.Id);
        Assert.Equal(TenantStatus.Deleted, tenant!.Status);
    }
}
