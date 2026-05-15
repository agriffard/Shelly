using CShells.Lifecycle;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TenantHub.Core.DTOs;
using TenantHub.Data;
using TenantHub.Data.Models;
using TenantHub.Data.Providers;

namespace TenantHub.Tests;

public class DatabaseShellSettingsProviderTests
{
    private static ServiceProvider CreateServiceProvider(string dbName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AdminDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task ListAsync_ReturnsActiveTenantsAndDefaultShell()
    {
        var sp = CreateServiceProvider(nameof(ListAsync_ReturnsActiveTenantsAndDefaultShell));
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

        db.Tenants.AddRange(
            new Tenant { Id = Guid.NewGuid(), Name = "Active", Slug = "active", Status = TenantStatus.Active, CreatedAt = DateTime.UtcNow },
            new Tenant { Id = Guid.NewGuid(), Name = "Suspended", Slug = "suspended", Status = TenantStatus.Suspended, CreatedAt = DateTime.UtcNow },
            new Tenant { Id = Guid.NewGuid(), Name = "Deleted", Slug = "deleted", Status = TenantStatus.Deleted, CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var provider = new DatabaseShellSettingsProvider(sp);
        var page = await provider.ListAsync(new BlueprintListQuery(null, 100, null));

        Assert.Equal(2, page.Items.Count);
        Assert.Contains(page.Items, x => x.Name == "active");
        Assert.Contains(page.Items, x => x.Name == "Default");
    }

    [Fact]
    public async Task GetAsync_AlwaysIncludesCoreFeature()
    {
        var sp = CreateServiceProvider(nameof(GetAsync_AlwaysIncludesCoreFeature));
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "Test",
            Slug = "test",
            Status = TenantStatus.Active,
            CreatedAt = DateTime.UtcNow,
            Features = [new TenantFeature { Id = Guid.NewGuid(), FeatureName = "Notes", IsEnabled = true, UpdatedAt = DateTime.UtcNow }]
        });
        await db.SaveChangesAsync();

        var provider = new DatabaseShellSettingsProvider(sp);
        var provided = await provider.GetAsync("test");
        var settings = await provided.Blueprint.ComposeAsync();

        Assert.Contains("Core", settings.EnabledFeatures);
        Assert.Contains("Notes", settings.EnabledFeatures);
    }

    [Fact]
    public async Task GetAsync_ThrowsForSuspended()
    {
        var sp = CreateServiceProvider(nameof(GetAsync_ThrowsForSuspended));
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

        db.Tenants.Add(new Tenant { Id = Guid.NewGuid(), Name = "Suspended", Slug = "suspended", Status = TenantStatus.Suspended, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var provider = new DatabaseShellSettingsProvider(sp);

        await Assert.ThrowsAsync<ShellBlueprintNotFoundException>(() => provider.GetAsync("suspended"));
    }

    [Fact]
    public async Task GetAsync_SetsCorrectConfigurationData()
    {
        var sp = CreateServiceProvider(nameof(GetAsync_SetsCorrectConfigurationData));
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

        db.Tenants.Add(new Tenant { Id = Guid.NewGuid(), Name = "Acme", Slug = "acme", Status = TenantStatus.Active, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var provider = new DatabaseShellSettingsProvider(sp);
        var provided = await provider.GetAsync("acme");
        var settings = await provided.Blueprint.ComposeAsync();

        Assert.Equal("acme", settings.ConfigurationData["WebRouting:Path"]);
        Assert.Equal("acme", settings.ConfigurationData["TenantSchema"]);
    }

    [Fact]
    public async Task GetAsync_DefaultShell_ReturnsCoreOnly()
    {
        var sp = CreateServiceProvider(nameof(GetAsync_DefaultShell_ReturnsCoreOnly));
        var provider = new DatabaseShellSettingsProvider(sp);

        var provided = await provider.GetAsync("Default");
        var settings = await provided.Blueprint.ComposeAsync();

        Assert.Equal("Default", settings.Id.Name);
        Assert.Single(settings.EnabledFeatures);
        Assert.Equal("Core", settings.EnabledFeatures[0]);
    }
}
