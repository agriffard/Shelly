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
    public async Task GetShellSettingsAsync_ReturnsOnlyActiveTenants()
    {
        var sp = CreateServiceProvider(nameof(GetShellSettingsAsync_ReturnsOnlyActiveTenants));
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

        db.Tenants.AddRange(
            new Tenant { Id = Guid.NewGuid(), Name = "Active", Slug = "active", Status = TenantStatus.Active, CreatedAt = DateTime.UtcNow },
            new Tenant { Id = Guid.NewGuid(), Name = "Suspended", Slug = "suspended", Status = TenantStatus.Suspended, CreatedAt = DateTime.UtcNow },
            new Tenant { Id = Guid.NewGuid(), Name = "Deleted", Slug = "deleted", Status = TenantStatus.Deleted, CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var provider = new DatabaseShellSettingsProvider(sp);
        var settings = (await provider.GetShellSettingsAsync()).ToList();

        Assert.Single(settings);
        Assert.Equal("active", settings[0].Id.Name);
    }

    [Fact]
    public async Task GetShellSettingsAsync_AlwaysIncludesCoreFeature()
    {
        var sp = CreateServiceProvider(nameof(GetShellSettingsAsync_AlwaysIncludesCoreFeature));
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new Tenant
        {
            Id = tenantId, Name = "Test", Slug = "test", Status = TenantStatus.Active, CreatedAt = DateTime.UtcNow,
            Features = [new TenantFeature { Id = Guid.NewGuid(), FeatureName = "Notes", IsEnabled = true, UpdatedAt = DateTime.UtcNow }]
        });
        await db.SaveChangesAsync();

        var provider = new DatabaseShellSettingsProvider(sp);
        var settings = (await provider.GetShellSettingsAsync()).ToList();

        Assert.Single(settings);
        Assert.Contains("Core", settings[0].EnabledFeatures);
        Assert.Contains("Notes", settings[0].EnabledFeatures);
    }

    [Fact]
    public async Task GetShellSettingsAsync_ByShellId_ReturnsNullForSuspended()
    {
        var sp = CreateServiceProvider(nameof(GetShellSettingsAsync_ByShellId_ReturnsNullForSuspended));
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

        db.Tenants.Add(new Tenant { Id = Guid.NewGuid(), Name = "Suspended", Slug = "suspended", Status = TenantStatus.Suspended, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var provider = new DatabaseShellSettingsProvider(sp);
        var result = await provider.GetShellSettingsAsync(new CShells.ShellId("suspended"));

        Assert.Null(result);
    }

    [Fact]
    public async Task GetShellSettingsAsync_SetsCorrectConfigurationData()
    {
        var sp = CreateServiceProvider(nameof(GetShellSettingsAsync_SetsCorrectConfigurationData));
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

        db.Tenants.Add(new Tenant { Id = Guid.NewGuid(), Name = "Acme", Slug = "acme", Status = TenantStatus.Active, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var provider = new DatabaseShellSettingsProvider(sp);
        var settings = (await provider.GetShellSettingsAsync()).First();

        Assert.Equal("acme", settings.ConfigurationData["WebRouting:Path"]);
        Assert.Equal("acme", settings.ConfigurationData["TenantSchema"]);
    }
}
